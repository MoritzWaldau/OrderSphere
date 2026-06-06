<#
.SYNOPSIS
    Seeds development passwords for the demo users in the imported `ordersphere` Keycloak realm.

.DESCRIPTION
    The realm file (ordersphere-realm.json) ships demo users with empty credentials so that no
    password is ever committed to source control. After Aspire imports the realm on first start,
    run this script once to set a known development password for every human demo user, so you can
    sign in through the BFF.

    Service-account users (username prefix `service-account-`) are skipped — their credentials are
    client secrets, not passwords.

    Development use only. Never run against a non-local Keycloak.

.PARAMETER KeycloakUrl
    Base URL of the local Keycloak instance. Default: http://localhost:8080

.PARAMETER Realm
    Target realm. Default: ordersphere

.PARAMETER AdminUser
    Keycloak master-realm admin username. Default: admin

.PARAMETER AdminPassword
    Keycloak master-realm admin password. Must match the value set in user-secrets
    (Parameters:keycloak-admin-password) for the AppHost.

.PARAMETER Password
    Password to assign to every demo user. Default: Password123!

.EXAMPLE
    ./seed-dev-passwords.ps1 -AdminPassword admin

.EXAMPLE
    ./seed-dev-passwords.ps1 -AdminPassword admin -Password 'Dev!2026'
#>
[CmdletBinding()]
param(
    [string]$KeycloakUrl = 'http://localhost:8080',
    [string]$Realm = 'ordersphere',
    [string]$AdminUser = 'admin',
    [Parameter(Mandatory = $true)][string]$AdminPassword,
    [string]$Password = 'Password123!'
)

$ErrorActionPreference = 'Stop'
$KeycloakUrl = $KeycloakUrl.TrimEnd('/')

Write-Host "Requesting admin token from $KeycloakUrl ..." -ForegroundColor Cyan
$tokenResponse = Invoke-RestMethod -Method Post `
    -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
    -ContentType 'application/x-www-form-urlencoded' `
    -Body @{
        client_id  = 'admin-cli'
        grant_type = 'password'
        username   = $AdminUser
        password   = $AdminPassword
    }

$headers = @{ Authorization = "Bearer $($tokenResponse.access_token)" }

Write-Host "Fetching users in realm '$Realm' ..." -ForegroundColor Cyan
$users = Invoke-RestMethod -Method Get `
    -Uri "$KeycloakUrl/admin/realms/$Realm/users?max=200" `
    -Headers $headers

$resetBody = @{ type = 'password'; value = $Password; temporary = $false } | ConvertTo-Json

$seeded = 0
foreach ($user in $users) {
    if ($user.username -like 'service-account-*') { continue }

    Invoke-RestMethod -Method Put `
        -Uri "$KeycloakUrl/admin/realms/$Realm/users/$($user.id)/reset-password" `
        -Headers $headers `
        -ContentType 'application/json' `
        -Body $resetBody | Out-Null

    Write-Host "  set password for $($user.username)" -ForegroundColor Green
    $seeded++
}

Write-Host "Done. Seeded $seeded user(s) with password '$Password'." -ForegroundColor Cyan
