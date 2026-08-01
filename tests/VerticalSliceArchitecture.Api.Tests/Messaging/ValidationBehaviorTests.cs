using FluentValidation;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.Messaging.Behavior;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Tests.Messaging;

public class ValidationBehaviorTests
{
	private sealed record FakeCommand(string Name) : IRequest<Result<FakeResponse>>;
	private sealed record FakeResponse(string Name);
	private sealed record FakeVoidCommand(string Name) : IRequest<Result>;

	private sealed class FakeCommandValidator : AbstractValidator<FakeCommand>
	{
		public FakeCommandValidator()
		{
			RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
		}
	}

	private sealed class FakeVoidCommandValidator : AbstractValidator<FakeVoidCommand>
	{
		public FakeVoidCommandValidator()
		{
			RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
		}
	}

	[Fact]
	public async Task Handle_NoValidatorsRegistered_CallsNext()
	{
		var behavior = new ValidationBehavior<FakeCommand, Result<FakeResponse>>([]);
		var expected = Result.Success(new FakeResponse("ok"));

		var result = await behavior.Handle(new FakeCommand("value"), () => Task.FromResult(expected), CancellationToken.None);

		Assert.Same(expected, result);
	}

	[Fact]
	public async Task Handle_ValidRequest_CallsNext()
	{
		var behavior = new ValidationBehavior<FakeCommand, Result<FakeResponse>>([new FakeCommandValidator()]);
		var expected = Result.Success(new FakeResponse("ok"));
		var nextCalled = false;

		var result = await behavior.Handle(new FakeCommand("value"), () =>
		{
			nextCalled = true;
			return Task.FromResult(expected);
		}, CancellationToken.None);

		Assert.True(nextCalled);
		Assert.Same(expected, result);
	}

	[Fact]
	public async Task Handle_InvalidRequest_ReturnsFailureResultOfT_WithoutCallingNext()
	{
		var behavior = new ValidationBehavior<FakeCommand, Result<FakeResponse>>([new FakeCommandValidator()]);
		var nextCalled = false;

		var result = await behavior.Handle(new FakeCommand(""), () =>
		{
			nextCalled = true;
			return Task.FromResult(Result.Success(new FakeResponse("unreachable")));
		}, CancellationToken.None);

		Assert.False(nextCalled);
		Assert.True(result.IsFailure);
		Assert.Equal(ErrorType.Validation, result.Error.Type);
		Assert.Contains("Name is required.", result.Error.Description);
	}

	[Fact]
	public async Task Handle_InvalidRequest_ReturnsFailurePlainResult_WithoutCallingNext()
	{
		var behavior = new ValidationBehavior<FakeVoidCommand, Result>([new FakeVoidCommandValidator()]);
		var nextCalled = false;

		var result = await behavior.Handle(new FakeVoidCommand(""), () =>
		{
			nextCalled = true;
			return Task.FromResult(Result.Success());
		}, CancellationToken.None);

		Assert.False(nextCalled);
		Assert.True(result.IsFailure);
		Assert.Equal(ErrorType.Validation, result.Error.Type);
	}
}
