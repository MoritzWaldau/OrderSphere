using System.Net;
using System.Net.Http.Json;

namespace OrderSphere.Web.Services;

/// <summary>Coarse category of an API failure, so the UI can react beyond a generic message.</summary>
public enum ApiErrorKind
{
    Network,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Server,
    Unknown,
}

/// <summary>A structured API error carrying a user-facing message and the originating status.</summary>
public sealed record ApiError(ApiErrorKind Kind, string Message, int? StatusCode = null)
{
    public static readonly ApiError Network =
        new(ApiErrorKind.Network, "Keine Verbindung zum Server. Bitte versuche es erneut.");
}

/// <summary>Result of an API call that returns no payload.</summary>
public sealed record ApiResult(bool IsSuccess, ApiError? Error)
{
    public static ApiResult Ok() => new(true, null);
    public static ApiResult Fail(ApiError error) => new(false, error);
}

/// <summary>Result of an API call that returns a payload of type <typeparamref name="T"/>.</summary>
public sealed record ApiResult<T>(bool IsSuccess, T? Value, ApiError? Error)
{
    public static ApiResult<T> Ok(T value) => new(true, value, null);
    public static ApiResult<T> Fail(ApiError error) => new(false, default, error);
}

/// <summary>Maps an <see cref="HttpResponseMessage"/> to an <see cref="ApiResult"/>, reading a
/// ProblemDetails-style body for a server-provided message when available.</summary>
public static class ApiResultExtensions
{
    public static async Task<ApiResult> ToApiResultAsync(
        this HttpResponseMessage response, CancellationToken ct = default)
    {
        return response.IsSuccessStatusCode
            ? ApiResult.Ok()
            : ApiResult.Fail(await BuildErrorAsync(response, ct));
    }

    public static async Task<ApiResult<T>> ToApiResultAsync<T>(
        this HttpResponseMessage response, CancellationToken ct = default)
    {
        if (!response.IsSuccessStatusCode)
            return ApiResult<T>.Fail(await BuildErrorAsync(response, ct));

        var value = await response.Content.ReadFromJsonAsync<T>(ct);
        return value is null
            ? ApiResult<T>.Fail(new ApiError(ApiErrorKind.Server, "Unerwartete Antwort vom Server."))
            : ApiResult<T>.Ok(value);
    }

    /// <summary>GETs <paramref name="url"/> and maps the result, turning a transport failure
    /// (no connection) into a structured Network error.</summary>
    public static async Task<ApiResult<T>> GetApiAsync<T>(
        this HttpClient http, string url, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync(url, ct);
            return await response.ToApiResultAsync<T>(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiResult<T>.Fail(ApiError.Network);
        }
    }

    /// <summary>Sends a prepared request expecting no payload, mapping transport failures to Network.</summary>
    public static async Task<ApiResult> SendApiAsync(
        this HttpClient http, HttpRequestMessage request, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.SendAsync(request, ct);
            return await response.ToApiResultAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiResult.Fail(ApiError.Network);
        }
    }

    /// <summary>Sends a prepared request expecting a payload, mapping transport failures to Network.</summary>
    public static async Task<ApiResult<T>> SendApiAsync<T>(
        this HttpClient http, HttpRequestMessage request, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.SendAsync(request, ct);
            return await response.ToApiResultAsync<T>(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiResult<T>.Fail(ApiError.Network);
        }
    }

    private static async Task<ApiError> BuildErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var kind = response.StatusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiErrorKind.Validation,
            HttpStatusCode.Unauthorized => ApiErrorKind.Unauthorized,
            HttpStatusCode.Forbidden => ApiErrorKind.Forbidden,
            HttpStatusCode.NotFound => ApiErrorKind.NotFound,
            HttpStatusCode.Conflict => ApiErrorKind.Conflict,
            >= HttpStatusCode.InternalServerError => ApiErrorKind.Server,
            _ => ApiErrorKind.Unknown,
        };

        var message = await ReadProblemMessageAsync(response, ct) ?? DefaultMessage(kind);
        return new ApiError(kind, message, (int)response.StatusCode);
    }

    private static string DefaultMessage(ApiErrorKind kind) => kind switch
    {
        ApiErrorKind.Validation => "Die Eingabe ist ungültig.",
        ApiErrorKind.Unauthorized => "Bitte melde dich an, um fortzufahren.",
        ApiErrorKind.Forbidden => "Dafür fehlt dir die Berechtigung.",
        ApiErrorKind.NotFound => "Die angeforderten Daten wurden nicht gefunden.",
        ApiErrorKind.Conflict => "Die Aktion steht im Konflikt mit dem aktuellen Stand.",
        ApiErrorKind.Server => "Auf dem Server ist ein Fehler aufgetreten. Bitte versuche es später erneut.",
        _ => "Die Aktion konnte nicht ausgeführt werden.",
    };

    private static async Task<string?> ReadProblemMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        // Backend failures may carry ProblemDetails or { detail/title/message }. Best-effort only.
        try
        {
            if (response.Content.Headers.ContentType?.MediaType is not
                ("application/json" or "application/problem+json"))
                return null;

            var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(ct);
            var text = problem?.Detail ?? problem?.Title ?? problem?.Message;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

    private sealed record ProblemBody(string? Detail, string? Title, string? Message);
}
