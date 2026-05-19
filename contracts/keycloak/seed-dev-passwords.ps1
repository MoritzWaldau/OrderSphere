# Setzt bekannte Dev-Passwörter für alle Mock-User via Keycloak Admin REST API.
# Nur für lokale Entwicklung. Nie in Produktion ausführen.
#
# Voraussetzung: Aspire App muss laufen (Keycloak auf http://localhost:8080)
#
# Aufruf:
#   .\seed-dev-passwords.ps1
#   .\seed-dev-passwords.ps1 -KeycloakBase "http://localhost:8080" -AdminPassword "admin"

param(
    [string]$KeycloakBase = "http://localhost:8080",
    [string]$AdminUser    = "admin",
    [string]$AdminPassword = "admin",
    [string]$Realm        = "ordersphere"
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Dev-Passwörter (ändern nach Belieben, aber nie echte Passwörter eintragen)
# ---------------------------------------------------------------------------
$users = @(
    @{ username = "moritz.waldau@ordersphere.dev";  password = "Admin1234!" }
    @{ username = "max.mustermann@ordersphere.dev"; password = "Kunde1234!" }
    @{ username = "anna.schmidt@ordersphere.dev";   password = "Kunde1234!" }
    @{ username = "jonas.bauer@ordersphere.dev";    password = "Kunde1234!" }
    @{ username = "test.admin@ordersphere.dev";     password = "Admin1234!" }
)

function Wait-ForKeycloak {
    Write-Host "Warte auf Keycloak..." -ForegroundColor Yellow
    $attempts = 0
    while ($attempts -lt 30) {
        try {
            $r = Invoke-WebRequest -Uri "$KeycloakBase/realms/master" -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -eq 200) {
                Write-Host "Keycloak ist bereit." -ForegroundColor Green
                return
            }
        } catch { }
        $attempts++
        Start-Sleep -Seconds 2
    }
    throw "Keycloak nicht erreichbar nach 60 Sekunden."
}

function Get-AdminToken {
    $body = @{
        grant_type = "password"
        client_id  = "admin-cli"
        username   = $AdminUser
        password   = $AdminPassword
    }
    $r = Invoke-RestMethod `
        -Uri "$KeycloakBase/realms/master/protocol/openid-connect/token" `
        -Method POST `
        -Body $body `
        -ContentType "application/x-www-form-urlencoded"
    return $r.access_token
}

function Get-UserId([string]$token, [string]$username) {
    $encoded = [System.Web.HttpUtility]::UrlEncode($username)
    $r = Invoke-RestMethod `
        -Uri "$KeycloakBase/admin/realms/$Realm/users?username=$encoded&exact=true" `
        -Headers @{ Authorization = "Bearer $token" }
    if ($r.Count -eq 0) { throw "User '$username' nicht gefunden im Realm '$Realm'." }
    return $r[0].id
}

function Set-Password([string]$token, [string]$userId, [string]$password) {
    $body = @{
        type      = "password"
        value     = $password
        temporary = $false
    } | ConvertTo-Json
    Invoke-RestMethod `
        -Uri "$KeycloakBase/admin/realms/$Realm/users/$userId/reset-password" `
        -Method PUT `
        -Headers @{ Authorization = "Bearer $token" } `
        -Body $body `
        -ContentType "application/json"
}

# ---------------------------------------------------------------------------
Wait-ForKeycloak

Add-Type -AssemblyName System.Web

Write-Host "Admin-Token wird geholt..." -ForegroundColor Cyan
$token = Get-AdminToken

foreach ($u in $users) {
    try {
        $id = Get-UserId -token $token -username $u.username
        Set-Password -token $token -userId $id -password $u.password
        Write-Host "  OK  $($u.username)" -ForegroundColor Green
    } catch {
        Write-Host "  FEHLER  $($u.username): $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Fertig. Dev-Zugangsdaten:" -ForegroundColor Cyan
Write-Host "  Admin:   moritz.waldau@ordersphere.dev  /  Admin1234!"
Write-Host "  Admin:   test.admin@ordersphere.dev     /  Admin1234!"
Write-Host "  Kunde:   max.mustermann@ordersphere.dev /  Kunde1234!"
Write-Host "  Kunde:   anna.schmidt@ordersphere.dev   /  Kunde1234!"
Write-Host "  Kunde:   jonas.bauer@ordersphere.dev    /  Kunde1234!"
