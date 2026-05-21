using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.BuildingBlocks.Extensions;

public static class ResultExtensions
{
    public static TResult Match<T, TResult>(
        this Result<T> result,
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Error);
    }

    public static void Match<T>(
        this Result<T> result,
        Action<T> onSuccess,
        Action<Error> onFailure)
    {
        if (result.IsSuccess)
            onSuccess(result.Value);
        else
            onFailure(result.Error);
    }
}
