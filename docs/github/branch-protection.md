# Branch Protection & Required Checks (master)

Diese Regeln setzen die in [branching.md](branching.md) § „Branch Protection" beschriebene Strategie
durch. Sie sind **Repository-Einstellungen** und nicht über committete Dateien erzwingbar — daher hier
als versioniertes Ruleset (`branch-protection.json`) plus Anwendungsbefehl dokumentiert.

## Regeln

- Pull Request für jeden Merge erforderlich, ≥ 1 Approval, Code-Owner-Review (siehe `.github/CODEOWNERS`).
- Lineare Historie (Squash-Merge), keine Force-Pushes, kein Löschen von `master`.
- Stale Reviews werden bei neuem Push verworfen; offene Review-Threads müssen aufgelöst sein.
- Required Status Checks (müssen grün sein, Branch muss aktuell sein):

  | Check (Context)               | Quelle |
  |-------------------------------|--------|
  | `Format`                      | ci.yml |
  | `Build & Test`                | ci.yml |
  | `Vulnerable Packages`         | ci.yml |
  | `Analyze (csharp)`            | codeql.yml |
  | `Dependency Review`           | dependency-review.yml |
  | `Validate PR title`           | pr-title-lint.yml |
  | `SonarQube Cloud Code Analysis` | SonarCloud-App |

  > Die exakten Context-Namen müssen mit den real erzeugten Checks übereinstimmen. Nach dem ersten
  > Lauf der Workflows in der GitHub-UI (Branch protection → Status checks) verifizieren; der
  > SonarCloud-Context kann je nach App-Konfiguration abweichen.

## Anwenden

Voraussetzung: `gh` authentifiziert, Admin-Rechte auf dem Repo.

```bash
gh api \
  --method POST \
  -H "Accept: application/vnd.github+json" \
  /repos/MoritzWaldau/OrderSphere/rulesets \
  --input docs/github/branch-protection.json
```

Aktualisieren eines bestehenden Rulesets (ID zuvor via `gh api /repos/MoritzWaldau/OrderSphere/rulesets` ermitteln):

```bash
gh api --method PUT \
  -H "Accept: application/vnd.github+json" \
  /repos/MoritzWaldau/OrderSphere/rulesets/<RULESET_ID> \
  --input docs/github/branch-protection.json
```

## Environments

- `dev` — kein Approval-Gate (automatischer/manueller Dev-Deploy).
- `staging`, `production` — beim Aufbau von Track D mit **Required Reviewers** und ggf.
  Wait-Timer versehen (Settings → Environments). Production-Deploys laufen nur nach manueller Freigabe.
