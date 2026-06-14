namespace OrderSphere.BuildingBlocks.Security;

/// <summary>
/// Provides identity information about the caller of the current request.
/// Resolved from the JWT / cookie principal at the API layer; returns an
/// unauthenticated stub in worker / background contexts.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Auth0 subject claim ("sub"). Null when not authenticated.</summary>
    string? Sub { get; }

    /// <summary>Display name from "preferred_username" claim. Null when not authenticated.</summary>
    string? Name { get; }

    /// <summary>Email address from the "email" claim. Null when not authenticated.</summary>
    string? Email { get; }

    /// <summary>Roles granted to the caller.</summary>
    IReadOnlyList<string> Roles { get; }

    bool IsAuthenticated { get; }

    /// <summary>Returns true when the caller holds at least one of the supplied roles.</summary>
    bool IsInRole(params string[] roles);
}
