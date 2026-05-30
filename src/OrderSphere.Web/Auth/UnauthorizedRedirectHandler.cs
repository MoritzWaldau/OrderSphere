using Microsoft.AspNetCore.Components;

namespace OrderSphere.Web.Auth;

/// <summary>
/// Intercepts 401 responses from the BFF and redirects to /bff/login with a returnUrl.
/// Placed after AntiforgeryDelegatingHandler in the pipeline so CSRF headers are already attached.
/// </summary>
public sealed class UnauthorizedRedirectHandler(NavigationManager navigation) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var returnUrl = navigation.Uri;
            navigation.NavigateTo($"/bff/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
        }

        return response;
    }
}
