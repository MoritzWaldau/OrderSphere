namespace OrderSphere.BuildingBlocks.Primitives;

public enum ErrorType
{
    Failure = 0,   // → 400 BadRequest
    Validation = 1,   // → 400 BadRequest (ValidationProblemDetails)
    NotFound = 2,   // → 404 NotFound
    Conflict = 3,   // → 409 Conflict
    Unauthorized = 4,   // → 401 Unauthorized
    Forbidden = 5,   // → 403 Forbidden
    Unexpected = 6,   // → 500 Problem
}
