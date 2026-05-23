using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace OrderSphere.RealmContract.Tests;

/// <summary>
/// Contract tests against <c>contracts/keycloak/ordersphere-realm.json</c>.
///
/// These tests act as a regression gate on the realm definition: any PR that
/// accidentally enables a dangerous setting (e.g. direct access grants, missing
/// MFA flow, weakened password policy) is caught before reaching staging.
///
/// The realm JSON is copied to the test output directory by the .csproj.
/// </summary>
public sealed class RealmContractTests
{
    private static readonly JsonNode Realm = LoadRealm();

    private static JsonNode LoadRealm()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "ordersphere-realm.json");

        var json = File.ReadAllText(path);
        return JsonNode.Parse(json)
            ?? throw new InvalidOperationException("Failed to parse ordersphere-realm.json");
    }

    // ── Realm identity ────────────────────────────────────────────────────────

    [Fact]
    public void Realm_NameIs_ordersphere()
    {
        Realm["realm"]!.GetValue<string>().Should().Be("ordersphere");
    }

    // ── Password policy ───────────────────────────────────────────────────────

    [Fact]
    public void PasswordPolicy_MatchesExpectedString()
    {
        const string expected =
            "length(12) and upperCase(1) and lowerCase(1) and digits(1) " +
            "and specialChars(1) and notUsername and notEmail and passwordHistory(5)";

        Realm["passwordPolicy"]!.GetValue<string>().Should().Be(expected);
    }

    // ── Brute-force protection ────────────────────────────────────────────────

    [Fact]
    public void BruteForceProtection_IsEnabled()
    {
        Realm["bruteForceProtected"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void PermanentLockout_IsEnabled()
    {
        Realm["permanentLockout"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void FailureFactor_IsAtMostTen()
    {
        // failureFactor = failures per interval; combined with maxTemporaryLockouts
        // this produces the effective lockout threshold.
        Realm["failureFactor"]!.GetValue<int>().Should().BeLessOrEqualTo(10);
    }

    // ── Browser flow ──────────────────────────────────────────────────────────

    [Fact]
    public void BrowserFlow_IsConditionalOtp()
    {
        Realm["browserFlow"]!.GetValue<string>().Should().Be("browser-with-conditional-otp");
    }

    // ── Clients — existence ───────────────────────────────────────────────────

    [Theory]
    [InlineData("web-bff")]
    [InlineData("ordering-api")]
    [InlineData("catalog-api")]
    [InlineData("userprofile-api")]
    [InlineData("ordering-worker")]
    [InlineData("notification-worker")]
    public void Client_Exists(string clientId)
    {
        GetClient(clientId).Should().NotBeNull(
            $"client '{clientId}' must be defined in the realm");
    }

    // ── Clients — directAccessGrantsEnabled must be false ────────────────────

    [Theory]
    [InlineData("web-bff")]
    [InlineData("ordering-api")]
    [InlineData("catalog-api")]
    [InlineData("userprofile-api")]
    [InlineData("ordering-worker")]
    [InlineData("notification-worker")]
    public void Client_DirectAccessGrantsEnabled_IsFalse(string clientId)
    {
        // directAccessGrantsEnabled = resource owner password credentials grant.
        // This grant is insecure for production clients and must be disabled.
        var client = GetClient(clientId);
        var value  = client?["directAccessGrantsEnabled"]?.GetValue<bool>() ?? false;
        value.Should().BeFalse(
            $"client '{clientId}' must not enable the resource owner password grant");
    }

    // ── Clients — bearer-only resource servers must have standardFlowEnabled=false ──

    [Theory]
    [InlineData("ordering-api")]
    [InlineData("catalog-api")]
    [InlineData("userprofile-api")]
    public void ResourceServerClient_StandardFlowEnabled_IsFalse(string clientId)
    {
        var client = GetClient(clientId);
        var value  = client?["standardFlowEnabled"]?.GetValue<bool>() ?? false;
        value.Should().BeFalse(
            $"resource server '{clientId}' is bearer-only and must not allow the authorization code flow");
    }

    // ── Clients — workers must have serviceAccountsEnabled ───────────────────

    [Theory]
    [InlineData("ordering-worker")]
    [InlineData("notification-worker")]
    public void WorkerClient_ServiceAccountsEnabled_IsTrue(string clientId)
    {
        var client = GetClient(clientId);
        var value  = client?["serviceAccountsEnabled"]?.GetValue<bool>() ?? false;
        value.Should().BeTrue(
            $"worker client '{clientId}' must have service accounts enabled for client_credentials grant");
    }

    // ── Roles — existence ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("customer")]
    [InlineData("csr")]
    [InlineData("order-manager")]
    [InlineData("catalog-admin")]
    [InlineData("admin")]
    [InlineData("requires-mfa")]
    [InlineData("svc.ordering")]
    [InlineData("svc.notification")]
    public void RealmRole_Exists(string roleName)
    {
        GetRole(roleName).Should().NotBeNull(
            $"realm role '{roleName}' must exist");
    }

    // ── Staff roles must include requires-mfa as a composite ─────────────────

    [Theory]
    [InlineData("csr")]
    [InlineData("order-manager")]
    [InlineData("catalog-admin")]
    [InlineData("admin")]
    public void StaffRole_IncludesRequiresMfaComposite(string roleName)
    {
        var role = GetRole(roleName)!;

        role["composite"]!.GetValue<bool>().Should().BeTrue(
            $"'{roleName}' must be composite to include requires-mfa");

        var compositeRealm = role["composites"]?["realm"]?.AsArray();
        compositeRealm.Should().NotBeNull();

        var includesMfa = compositeRealm!
            .Select(n => n!.GetValue<string>())
            .Contains("requires-mfa");

        includesMfa.Should().BeTrue(
            $"'{roleName}' must include 'requires-mfa' to trigger the MFA conditional flow");
    }

    // ── Authentication flows ──────────────────────────────────────────────────

    [Theory]
    [InlineData("browser-with-conditional-otp")]
    [InlineData("browser-conditional-otp-forms")]
    [InlineData("browser-conditional-otp-2fa")]
    public void AuthenticationFlow_Exists(string alias)
    {
        var flows = Realm["authenticationFlows"]?.AsArray();
        flows.Should().NotBeNull();

        var found = flows!.Any(f =>
            f?["alias"]?.GetValue<string>() == alias);

        found.Should().BeTrue(
            $"authentication flow '{alias}' must be defined");
    }

    [Fact]
    public void MfaConditionalFlow_ContainsConditionUserRoleAuthenticator()
    {
        var flow = GetAuthFlow("browser-conditional-otp-2fa");
        flow.Should().NotBeNull();

        var executions = flow!["authenticationExecutions"]?.AsArray();
        executions.Should().NotBeNull();

        var hasCondition = executions!.Any(e =>
            e?["authenticator"]?.GetValue<string>() == "condition-user-role");

        hasCondition.Should().BeTrue(
            "the MFA conditional flow must contain a condition-user-role authenticator");
    }

    [Fact]
    public void MfaConditionalFlow_ContainsWebAuthnAlternative()
    {
        var flow       = GetAuthFlow("browser-conditional-otp-2fa");
        var executions = flow!["authenticationExecutions"]!.AsArray();

        var hasWebAuthn = executions.Any(e =>
            e?["authenticator"]?.GetValue<string>() == "webauthn-authenticator");

        hasWebAuthn.Should().BeTrue(
            "WebAuthn must be available as an MFA alternative");
    }

    [Fact]
    public void MfaConditionalFlow_ContainsOtpAlternative()
    {
        var flow       = GetAuthFlow("browser-conditional-otp-2fa");
        var executions = flow!["authenticationExecutions"]!.AsArray();

        var hasOtp = executions.Any(e =>
            e?["authenticator"]?.GetValue<string>() == "auth-otp-form");

        hasOtp.Should().BeTrue(
            "OTP (TOTP) must be available as an MFA alternative");
    }

    // ── Token lifespans ───────────────────────────────────────────────────────

    [Fact]
    public void AccessTokenLifespan_IsAtMost300Seconds()
    {
        // Short-lived access tokens (≤ 5 min) limit the blast radius of token leakage.
        var lifespan = Realm["accessTokenLifespan"]!.GetValue<int>();
        lifespan.Should().BeLessOrEqualTo(300);
    }

    [Fact]
    public void SsoSessionIdleTimeout_IsAtMost1800Seconds()
    {
        // 30-minute idle timeout aligns with the BFF cookie sliding expiration.
        var timeout = Realm["ssoSessionIdleTimeout"]!.GetValue<int>();
        timeout.Should().BeLessOrEqualTo(1800);
    }

    [Fact]
    public void OfflineSessionMaxLifespanEnabled_IsFalse()
    {
        // Offline tokens are not used and must remain disabled.
        Realm["offlineSessionMaxLifespanEnabled"]!.GetValue<bool>().Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JsonNode? GetClient(string clientId)
    {
        var clients = Realm["clients"]?.AsArray();
        return clients?.FirstOrDefault(c =>
            c?["clientId"]?.GetValue<string>() == clientId);
    }

    private static JsonNode? GetRole(string roleName)
    {
        var roles = Realm["roles"]?["realm"]?.AsArray();
        return roles?.FirstOrDefault(r =>
            r?["name"]?.GetValue<string>() == roleName);
    }

    private static JsonNode? GetAuthFlow(string alias)
    {
        var flows = Realm["authenticationFlows"]?.AsArray();
        return flows?.FirstOrDefault(f =>
            f?["alias"]?.GetValue<string>() == alias);
    }
}
