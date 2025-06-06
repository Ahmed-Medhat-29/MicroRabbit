﻿using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MicroRabbit.Infra.Bus;

public sealed class RabbitMQBus : IEventBus
{
	private readonly IMediator _mediator;
	private readonly Dictionary<string, List<Type>> _handlers;
	private readonly List<Type> _eventTypes;

	public RabbitMQBus(IMediator mediator)
	{
		_mediator = mediator;
		_handlers = new Dictionary<string, List<Type>>();
		_eventTypes = new List<Type>();
	}

	public Task SendCommand<T>(T command) where T : Command
	{
		return _mediator.Send(command);
	}

	public async Task Publish<T>(T @event) where T : Event
	{
		var factory = new ConnectionFactory() { HostName = "localhost" };
		using var connection = await factory.CreateConnectionAsync();
		using var channel = await connection.CreateChannelAsync();

		var eventName = @event.GetType().Name;
		await channel.QueueDeclareAsync(eventName, false, false, false, null);

		var message = JsonSerializer.Serialize(@event);
		var body = Encoding.UTF8.GetBytes(message);

		await channel.BasicPublishAsync("", eventName, body);
	}

	public void Subscribe<T, TH>()
		where T : Event
		where TH : IEventHandler<T>
	{
		var eventName = typeof(T).Name;
		var handlerType = typeof(TH);

		if (!_eventTypes.Contains(typeof(T)))
		{
			_eventTypes.Add(typeof(T));
		}

		if (!_handlers.ContainsKey(eventName))
		{
			_handlers.Add(eventName, new List<Type>());
		}

		if (_handlers[eventName].Any(s => s.GetType() == handlerType))
		{
			throw new ArgumentException($"Handler Type {handlerType.Name} already is registered for '{eventName}'", nameof(handlerType));
		}

		_handlers[eventName].Add(handlerType);

		StartBasicConsume<T>().RunSynchronously();
	}

	private async Task StartBasicConsume<T>() where T : Event
	{
		var factory = new ConnectionFactory
		{
			HostName = "localhost"
		};

		var connection = await factory.CreateConnectionAsync();
		var channel = await connection.CreateChannelAsync();

		var eventName = typeof(T).Name;

		await channel.QueueDeclareAsync(eventName, false, false, false);


		var consumer = new AsyncEventingBasicConsumer(channel);
		consumer.ReceivedAsync += Consumer_Received;

		await channel.BasicConsumeAsync("", true, consumer);
	}

	private async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
	{
		var eventName = e.RoutingKey;
		var message = Encoding.UTF8.GetString(e.Body.ToArray());

		try
		{
			await ProcessEvent(eventName, message).ConfigureAwait(false);
		}
		catch (Exception)
		{

		}
	}

	private async Task ProcessEvent(string eventName, string message)
	{
		if (_handlers.ContainsKey(eventName))
		{
			var subscriptions = _handlers[eventName];
			foreach (var subscription in subscriptions)
			{
				var handler = Activator.CreateInstance(subscription);
				if (handler == null) continue;

				var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);

				var @event = JsonSerializer.Deserialize(message, eventType);

				var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

				await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
			}
		}
	}
}
