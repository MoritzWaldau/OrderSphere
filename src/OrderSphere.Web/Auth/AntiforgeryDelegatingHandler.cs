namespace OrderSphere.Web.Auth;

/// <summary>
/// DelegatingHandler that attaches the X-XSRF-TOKEN header to every
/// non-idempotent HTTP request. The token is provided by CsrfTokenService,
/// which is populated by BffAuthStateProvider on the initial /bff/user call.
/// Safe methods (GET, HEAD, OPTIONS, TRACE) pass through without modification.
/// </summary>
public sealed class AntiforgeryDelegatingHandler(CsrfTokenService csrfTokenService) : DelegatingHandler
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!SafeMethods.Contains(request.Method.Method))
        {
            var token = csrfTokenService.Token;
            if (!string.IsNullOrEmpty(token))
                request.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
