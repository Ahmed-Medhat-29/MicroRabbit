﻿using MediatR;
using MicroRabbit.Banking.Application.Interfaces;
using MicroRabbit.Banking.Application.Services;
using MicroRabbit.Banking.Data.Context;
using MicroRabbit.Banking.Data.Repository;
using MicroRabbit.Banking.Domain.CommandHandlers;
using MicroRabbit.Banking.Domain.Commands;
using MicroRabbit.Banking.Domain.Interfaces;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Infra.Bus;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MicroRabbit.Infra.IoC;

public class DependencyContainer
{
	public static void RegisterServices(IServiceCollection services)
	{
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
		
		// Domain Bus
		services.AddTransient<IEventBus, RabbitMQBus>();

		// Domain Banking Commands
		services.AddTransient<IRequestHandler<CreateTransferCommand, bool>, TransferCommandHandler>();

		// App Services
		services.AddTransient<IAccountService, AccountService>();

		// Data
		services.AddTransient<IAccountRepository, AccountRepository>();
	}
}
