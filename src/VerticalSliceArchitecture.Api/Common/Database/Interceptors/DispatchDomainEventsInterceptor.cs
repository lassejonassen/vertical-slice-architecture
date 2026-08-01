using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Domain.Common;

namespace VerticalSliceArchitecture.Api.Common.Database.Interceptors;

public sealed class DispatchDomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
	public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData,
		InterceptionResult<int> result,
		CancellationToken cancellationToken = default)
	{
		if (eventData.Context is not null)
		{
			await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
		}

		return await base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
	{
		var domainEntities = context.ChangeTracker
			.Entries<IHasDomainEvents>()
			.Where(x => x.Entity.DomainEvents.Any())
			.ToList();

		var domainEvents = domainEntities
			.SelectMany(x => x.Entity.DomainEvents)
			.ToList();

		domainEntities.ForEach(x => x.Entity.ClearDomainEvents());

		foreach (var domainEvent in domainEvents)
		{
			await publisher.Publish(domainEvent, cancellationToken);
		}
	}
}