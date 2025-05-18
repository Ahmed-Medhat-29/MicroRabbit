using MicroRabbit.Domain.Core.Events;

namespace MicroRabbit.Domain.Core.Bus;

public interface IEventHandler<TEvent> : IEventHandler
	where TEvent : Event
{
	Task Handle(TEvent @event);

	TEvent Handle();
}

public interface IEventHandler
{

}
