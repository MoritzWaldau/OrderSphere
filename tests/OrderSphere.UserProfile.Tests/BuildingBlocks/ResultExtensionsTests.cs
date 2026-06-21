using FluentAssertions;
using OrderSphere.BuildingBlocks.Extensions;
using OrderSphere.BuildingBlocks.Primitives;
using Xunit;

namespace OrderSphere.UserProfile.Tests.BuildingBlocks;

public sealed class ResultExtensionsTests
{
    private static readonly Error SomeError = new("test.error", "test error");


    [Fact]
    public void Match_SuccessResult_InvokesOnSuccessAndReturnsValue()
    {
        var result = Result<int>.Success(42);

        var output = result.Match(
            onSuccess: v => v * 2,
            onFailure: _ => -1);

        output.Should().Be(84);
    }


    [Fact]
    public void Match_FailureResult_InvokesOnFailureAndReturnsValue()
    {
        var result = Result<int>.Failure(SomeError);

        var output = result.Match(
            onSuccess: _ => 1,
            onFailure: e => e.Code.Length);

        output.Should().Be(SomeError.Code.Length);
    }


    [Fact]
    public void MatchAction_SuccessResult_InvokesOnSuccessCallback()
    {
        var result = Result<string>.Success("hello");
        var successCalled = false;
        var failureCalled = false;

        result.Match(
            onSuccess: _ => successCalled = true,
            onFailure: _ => failureCalled = true);

        successCalled.Should().BeTrue();
        failureCalled.Should().BeFalse();
    }


    [Fact]
    public void MatchAction_FailureResult_InvokesOnFailureCallback()
    {
        var result = Result<string>.Failure(SomeError);
        var successCalled = false;
        var failureCalled = false;

        result.Match(
            onSuccess: _ => successCalled = true,
            onFailure: _ => failureCalled = true);

        successCalled.Should().BeFalse();
        failureCalled.Should().BeTrue();
    }
}
