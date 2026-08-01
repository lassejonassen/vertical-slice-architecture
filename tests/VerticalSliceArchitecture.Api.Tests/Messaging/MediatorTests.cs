using Microsoft.Extensions.DependencyInjection;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.ResultPattern;
using VerticalSliceArchitecture.Api.Domain.Common;

namespace VerticalSliceArchitecture.Api.Tests.Messaging;

public class MediatorTests
{
	public sealed record FakeCommand(string Name) : IRequest<Result<string>>;

	public sealed class FakeCommandHandler : IRequestHandler<FakeCommand, Result<string>>
	{
		public Task<Result<string>> Handle(FakeCommand request, CancellationToken cancellationToken) =>
			Task.FromResult(Result.Success($"handled:{request.Name}"));
	}

	public sealed class RecordingBehavior : IPipelineBehavior<FakeCommand, Result<string>>
	{
		public static readonly List<string> CallOrder = [];

		public async Task<Result<string>> Handle(
			FakeCommand request, RequestHandlerDelegate<Result<string>> next, CancellationToken cancellationToken)
		{
			CallOrder.Add("before");
			var result = await next();
			CallOrder.Add("after");
			return result;
		}
	}

	public sealed record FakeVoidCommand(string Name) : IRequest;

	public sealed class FakeVoidCommandHandler : IRequestHandler<FakeVoidCommand>
	{
		public static bool WasCalled { get; private set; }

		public Task<Unit> Handle(FakeVoidCommand request, CancellationToken cancellationToken)
		{
			WasCalled = true;
			return Task.FromResult(Unit.Value);
		}
	}

	public sealed record FakeEvent(Guid Id) : DomainEvent;

	public sealed class FakeEventHandlerOne : INotificationHandler<FakeEvent>
	{
		public static int CallCount;

		public Task Handle(FakeEvent notification, CancellationToken cancellationToken)
		{
			CallCount++;
			return Task.CompletedTask;
		}
	}

	public sealed class FakeEventHandlerTwo : INotificationHandler<FakeEvent>
	{
		public static int CallCount;

		public Task Handle(FakeEvent notification, CancellationToken cancellationToken)
		{
			CallCount++;
			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task Send_ResolvesHandlerAndRunsBehaviorsInRegistrationOrder()
	{
		RecordingBehavior.CallOrder.Clear();
		var services = new ServiceCollection();
		services.AddTransient<IRequestHandler<FakeCommand, Result<string>>, FakeCommandHandler>();
		services.AddTransient<IPipelineBehavior<FakeCommand, Result<string>>, RecordingBehavior>();
		var mediator = new Mediator(services.BuildServiceProvider());

		var result = await mediator.Send(new FakeCommand("value"));

		Assert.True(result.IsSuccess);
		Assert.Equal("handled:value", result.Value);
		Assert.Equal(["before", "after"], RecordingBehavior.CallOrder);
	}

	[Fact]
	public async Task Send_Void_ResolvesHandlerAndInvokesIt()
	{
		var services = new ServiceCollection();
		services.AddTransient<IRequestHandler<FakeVoidCommand>, FakeVoidCommandHandler>();
		var mediator = new Mediator(services.BuildServiceProvider());

		await mediator.Send(new FakeVoidCommand("value"));

		Assert.True(FakeVoidCommandHandler.WasCalled);
	}

	[Fact]
	public async Task Publish_ResolvesAllRegisteredNotificationHandlers()
	{
		FakeEventHandlerOne.CallCount = 0;
		FakeEventHandlerTwo.CallCount = 0;
		var services = new ServiceCollection();
		services.AddTransient<INotificationHandler<FakeEvent>, FakeEventHandlerOne>();
		services.AddTransient<INotificationHandler<FakeEvent>, FakeEventHandlerTwo>();
		var mediator = new Mediator(services.BuildServiceProvider());

		await mediator.Publish(new FakeEvent(Guid.NewGuid()));

		Assert.Equal(1, FakeEventHandlerOne.CallCount);
		Assert.Equal(1, FakeEventHandlerTwo.CallCount);
	}

	[Fact]
	public async Task Publish_NoHandlersRegistered_DoesNotThrow()
	{
		var mediator = new Mediator(new ServiceCollection().BuildServiceProvider());

		var exception = await Record.ExceptionAsync(() => mediator.Publish(new FakeEvent(Guid.NewGuid())));

		Assert.Null(exception);
	}
}
