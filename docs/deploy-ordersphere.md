# OrderSphere — Azure-Deployment (azd)

Bereitstellung von OrderSphere in eine eigene Azure-DEV-Umgebung über die Azure Developer CLI
(`azd`). OrderSphere ist eine .NET-Aspire-Anwendung: das AppHost-Manifest
(`src/OrderSphere.AppHost`) ist die einzige Quelle der Ressourcentopologie. `azd` liest das
Manifest und generiert daraus die Bicep-Vorlagen (Container Apps, PostgreSQL Flexible Server,
Service Bus, Azure Managed Redis, Key Vault). Es gibt bewusst **keinen** handgepflegten
`infra/`-Ordner.

Keycloak ist **nicht** Teil dieser Bereitstellung. Es läuft als unabhängiger zentraler
SSO-Provider (siehe [`deploy/sso/`](../deploy/sso/README.md)). Die Kopplung erfolgt ausschließlich
über die Issuer-URL und vier Client-Secrets.

## Voraussetzungen

- `azd` installiert (`azd version`), `dotnet` 10 SDK.
- Azure-Subscription mit Berechtigung, die Resource Group `rg-ordersphere-dev` anzulegen.
- Laufendes Cloud-Keycloak (Issuer-URL bekannt).
- Docker (für den lokalen Container-Build durch `azd`).

## Eckdaten

| Schlüssel | Wert |
|---|---|
| Environment | `dev` |
| Region | `northeurope` |
| Resource Group | `rg-dev` |
| BFF-FQDN | `ordersphere-bff.kindtree-ed135723.northeurope.azurecontainerapps.io` |
| Issuer | `https://keycloak.salmoncoast-4abe9a09.northeurope.azurecontainerapps.io/realms/ordersphere` |

> **Resource Group:** Der Aspire-azd-Pfad leitet die Resource Group aus dem Environment-Namen ab
> (`rg-<env>` → `rg-dev`) und ignoriert ein nachträglich gesetztes `AZURE_RESOURCE_GROUP`. Die
> Infrastruktur liegt daher in `rg-dev`. Damit `azd deploy` die Container Apps in dieselbe Gruppe
> schreibt, muss `AZURE_RESOURCE_GROUP=rg-dev` gesetzt sein (sonst 404 `ResourceGroupNotFound`).

## Datenbankschema

Jeder Service ruft beim Start `Database.Migrate()` auf (ungated, nicht auf Development beschränkt —
siehe z. B. `src/Services/Catalog/.../Program.cs`). In der Cloud migriert damit jeder Container beim
ersten Start sein eigenes Schema gegen den PostgreSQL Flexible Server. Kein separater
Migrationsschritt nötig.

## Redis-Authentifizierung (Entra ID)

`AddAzureManagedRedis` provisioniert Azure Managed Redis mit Microsoft-Entra-ID-Authentifizierung
(Access Keys deaktiviert); der Managed Identity wird eine Data-Access-Policy zugewiesen. Der von
Aspire injizierte Connection-String enthält **kein** Passwort. `Aspire.StackExchange.Redis` bringt
keine Entra-Token-Logik mit — eine rohe `ConnectionMultiplexer.Connect(connectionString)` scheitert
deshalb mit `NOAUTH - connection has not yet authenticated`.

Die Services bauen die Redis-Verbindung daher über `AddOrderSphereRedisAsync`
(`OrderSphere.ServiceDefaults/RedisExtensions.cs`): Bei einem Azure-Redis-Endpoint ohne Passwort holt
`Microsoft.Azure.StackExchangeRedis` über die User-Assigned Managed Identity (`AZURE_CLIENT_ID`) ein
Entra-Token und erneuert es automatisch. Der token-authentifizierte `IConnectionMultiplexer` wird von
DistributedCache (Catalog, Ordering, BFF), DataProtection-Key-Ring und SignalR-Backplane (BFF)
gemeinsam genutzt. Lokal (Aspire-Dev-Container) verbindet derselbe Code ohne Credentials.

## Schritt-für-Schritt

### 1. Anmelden und Environment anlegen
```powershell
azd auth login
azd env new dev --location northeurope --subscription <SUBSCRIPTION_ID>
azd env set AZURE_RESOURCE_GROUP rg-dev
```

> **Wichtig:** Aspire-Parameter mit Bindestrich (`keycloak-realm-authority`,
> `payment-bypass-providers`, die vier `*-secret`) dürfen **nicht** über `azd env set` gesetzt
> werden — `azd env set` schreibt in die `.env`, und dort sind Bindestriche in Variablennamen
> ungültig (`unexpected character "-" in variable name`). Diese Parameter fragt `azd up` beim
> ersten Deploy interaktiv ab und speichert sie korrekt (Nicht-Secrets in `config.json`, Secrets
> als Key-Vault-Referenz). Nur `AZURE_*`-Werte (Unterstriche) gehören in die `.env`.

### 2. Echte Client-Secrets erzeugen
Vier neue Zufalls-Secrets erzeugen (eines je confidential Client) und notieren:
```powershell
foreach ($c in 'web-bff','ordering-worker','notification-worker','payment-worker') {
  $s = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }))
  Write-Host "$c = $s"
}
```
Jeden Wert in der **Cloud-Keycloak-Admin-Konsole** setzen
(*Clients → \<client\> → Credentials → Regenerate/Set*). Die laufende Instanz hat den Realm bereits
in Postgres importiert; Änderungen an `contracts/keycloak/ordersphere-realm.json` werden **nicht**
automatisch übernommen.

> Hinweis: `contracts/keycloak/ordersphere-realm.json` behält die `*-dev-secret-change-in-prod`-
> Platzhalter. Echte Secrets gehören nicht ins Git — nur in Keycloak und in den Key Vault.

### 3. Bereitstellen
```powershell
azd up
```
`azd up` fragt nacheinander die Parameterwerte ab:
- `keycloak-realm-authority` → die Issuer-URL (siehe Eckdaten oben)
- `payment-bypass-providers` → `true`
- `bff-client-secret`, `ordering-worker-secret`, `notification-worker-secret`,
  `payment-worker-secret` → die in Schritt 2 erzeugten Werte (werden als Key-Vault-Secrets abgelegt)

Danach provisioniert es die Infrastruktur in `rg-ordersphere-dev` und deployt alle 12 Projekte als
Container Apps. Nur `ordersphere-bff` erhält ein externes Ingress (`WithExternalHttpEndpoints()` im
AppHost). Die Antworten werden gespeichert; spätere `azd up`-Läufe fragen nicht erneut.

### 6. Keycloak gegen die BFF-URL abgleichen
Nach dem Deploy die öffentliche BFF-FQDN ermitteln:
```powershell
azd show
```
Am Client `web-bff` in der Keycloak-Admin-Konsole ergänzen:
- Redirect URI: `https://<bff-fqdn>/*`
- Web Origin: `https://<bff-fqdn>`
- Post-Logout-Redirect-URI: `https://<bff-fqdn>/*`
- Backchannel-Logout-URL: `https://<bff-fqdn>/bff/backchannel-logout`

## Verifikation

1. `azd up` läuft fehlerfrei; Ressourcen in `rg-ordersphere-dev` vorhanden.
2. Service-Logs zeigen erfolgreichen JWKS-Abruf von der Keycloak-FQDN.
3. `https://<bff-fqdn>` → Login-Redirect zu Keycloak → nach Schritt 6 erfolgreicher Rücksprung.
4. Authentifizierter API-Call (Audience-Validierung pro Service) liefert 200.
5. `client_credentials`-Token für `ordering-worker`/`payment-worker` mit dem echten Secret →
   gültiges Token mit Rolle `svc.*`.

## CI/CD — `azd pipeline config`

Erst **nach** dem ersten erfolgreichen `azd up` ausführen. Der Befehl richtet OIDC
(Workload Identity Federation) ein, legt/verwendet den Service Principal, setzt die GitHub-
Repo-Variablen (`AZURE_ENV_NAME`, `AZURE_LOCATION`, `AZURE_SUBSCRIPTION_ID`) und propagiert die
Environment-Werte inkl. Secret-Referenzen. Er generiert den Workflow `.github/workflows/azure-dev.yml`.

```powershell
azd pipeline config
```

Hinweise für dieses Repo:

- **AppHost nicht im Repo-Root.** Der generierte `azure-dev.yml` nimmt den AppHost im Wurzelverzeichnis
  an. Hier liegt er unter `src/OrderSphere.AppHost`. In den Schritten **Provision Infrastructure**
  und **Deploy Application** muss daher `working-directory: ./src/OrderSphere.AppHost` ergänzt werden
  (siehe [Aspire-Doku zu Multi-Projekt-Workflows](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azd/aca-deployment-github-actions)).
- **master ist PR-geschützt.** `azd pipeline config` will den Workflow committen/pushen — auf einem
  Branch arbeiten und per PR mergen, nicht direkt auf master pushen.
- **Bestehende SSO-Credentials.** Das SSO-Deployment nutzt bereits eine OIDC-Federated-Credential und
  die `AZURE_*`-Repo-Secrets. `azd pipeline config` kann eine eigene Identität anlegen; falls die
  vorhandene wiederverwendet werden soll, die generierten Variablen entsprechend angleichen.
- Der Workflow läuft `azd provision`/`azd deploy` und erfordert das .NET-10-SDK (Container-Build durch azd).

## Troubleshooting (real aufgetreten beim ersten Deploy)

| Symptom | Ursache | Behebung |
|---|---|---|
| `ConflictError: A vault with the same name already exists in deleted state` | Ein früherer, abgebrochener `azd up` hat den Key Vault angelegt und beim Aufräumen gelöscht; Key-Vault-Soft-Delete reserviert den Namen weiter. | `az keyvault purge --name <vault> --location northeurope`, dann `azd up` erneut. |
| `empty dotnet configuration output` (Vorschlag: `EnableSdkContainerSupport`) | Verschleiert den echten Fehler des `dotnet publish` beim Container-Bau. Zwei beobachtete Auslöser: (a) Docker-Credential-Helper liefert keine ACR-Credentials; (b) bei parallelem Bau scheitert ein Publish. | (a) siehe nächste Zeile; (b) Solution vorab `dotnet build -c Release` (Outputs warm) bzw. Services einzeln `azd deploy <service>`. |
| `docker-credential-desktop.EXE get … credentials not found in native keychain` | `~/.docker/config.json` hat `credsStore: desktop`, aber der Desktop-Keychain liefert das ACR-Token nicht. | `credsStore`-Zeile aus `~/.docker/config.json` entfernen, dann `az acr login --name <acr>` (legt das Token base64 direkt in `config.json` ab). |
| Deploy-Schritt: `404 ResourceGroupNotFound: 'rg-ordersphere-dev'` | Infrastruktur liegt in `rg-dev` (azd-Default `rg-<env>`), `AZURE_RESOURCE_GROUP` zeigte auf eine nicht existierende Gruppe. | `azd env set AZURE_RESOURCE_GROUP rg-dev`, dann `azd up`. |
| `/bff/login` → `500`, Logs: `NOAUTH … connection has not yet authenticated` (Redis) | Azure Managed Redis erzwingt Entra-ID-Auth; rohe Redis-Verbindung sendet kein Token. | Siehe Abschnitt [Redis-Authentifizierung](#redis-authentifizierung-entra-id) — Verbindung über `AddOrderSphereRedisAsync`. |
