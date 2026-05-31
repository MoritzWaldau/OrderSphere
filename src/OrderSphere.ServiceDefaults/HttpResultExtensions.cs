using Microsoft.AspNetCore.Http;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.ServiceDefaults;

/// <summary>
/// Extension methods that translate a <see cref="Result{T}"/> or <see cref="Result"/> to an
/// <see cref="IResult"/> for use in Minimal API endpoints.
/// The HTTP status code is derived from <see cref="Error.Type"/>:
///   NotFound    → 404   |   Conflict     → 409
///   Forbidden   → 403   |   Unauthorized → 401
///   Unexpected  → 500   |   Failure / Validation → 400
/// </summary>
public static class HttpResultExtensions
{
    /// <summary>
    /// Maps a <see cref="Result{T}"/> to an <see cref="IResult"/>.
    /// On success, calls <paramref name="onSuccess"/> if provided, otherwise returns HTTP 200 OK
    /// with the value as the JSON body.
    /// </summary>
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess is not null ? onSuccess(result.Value) : Results.Ok(result.Value);

        return MapError(result.Error);
    }

    /// <summary>
    /// Maps a non-generic <see cref="Result"/> to an <see cref="IResult"/>.
    /// On success, calls <paramref name="onSuccess"/> if provided, otherwise returns HTTP 204 No Content.
    /// </summary>
    public static IResult ToHttpResult(
        this Result result,
        Func<IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess is not null ? onSuccess() : Results.NoContent();

        return MapError(result.Error);
    }

    private static IResult MapError(Error error) => error.Type switch
    {
        ErrorType.NotFound => Results.NotFound(new ErrorResponse(error.Code, error.Description)),
        ErrorType.Conflict => Results.Conflict(new ErrorResponse(error.Code, error.Description)),
        ErrorType.Forbidden => Results.Forbid(),
        ErrorType.Unauthorized => Results.Unauthorized(),
        ErrorType.Unexpected=> Results.Problem(detail: error.Description, statusCode: StatusCodes.Status500InternalServerError),
        _ => Results.BadRequest(new ErrorResponse(error.Code, error.Description)),
    };
}
