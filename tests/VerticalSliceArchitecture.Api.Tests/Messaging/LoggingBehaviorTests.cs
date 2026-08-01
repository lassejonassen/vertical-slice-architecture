using Microsoft.Extensions.Logging.Abstractions;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.Messaging.Behavior;
using VerticalSliceArchitecture.Api.Common.ResultPattern;

namespace VerticalSliceArchitecture.Api.Tests.Messaging;

public class LoggingBehaviorTests
{
	private sealed record FakeCommand(string Name) : IRequest<Result<FakeResponse>>;
	private sealed record FakeResponse(string Name);

	private static LoggingBehavior<FakeCommand, Result<FakeResponse>> CreateBehavior() =>
		new(NullLogger<LoggingBehavior<FakeCommand, Result<FakeResponse>>>.Instance);

	[Fact]
	public async Task Handle_Success_CallsNextAndReturnsSameResult()
	{
		var behavior = CreateBehavior();
		var expected = Result.Success(new FakeResponse("ok"));

		var result = await behavior.Handle(new FakeCommand("value"), () => Task.FromResult(expected), CancellationToken.None);

		Assert.Same(expected, result);
	}

	[Fact]
	public async Task Handle_Failure_CallsNextAndReturnsSameResult()
	{
		var behavior = CreateBehavior();
		var expected = Result.Failure<FakeResponse>(new Error("Some.Error", "It failed.", ErrorType.Failure));

		var result = await behavior.Handle(new FakeCommand("value"), () => Task.FromResult(expected), CancellationToken.None);

		Assert.Same(expected, result);
	}

	[Fact]
	public async Task Handle_NextThrows_PropagatesException()
	{
		var behavior = CreateBehavior();

		Task<Result<FakeResponse>> Next() => throw new InvalidOperationException("boom");

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			behavior.Handle(new FakeCommand("value"), Next, CancellationToken.None));
	}
}
