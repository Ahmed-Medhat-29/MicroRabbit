using MicroRabbit.Banking.Data.Context;
using MicroRabbit.Banking.Domain.Interfaces;
using MicroRabbit.Banking.Domain.Models;

namespace MicroRabbit.Banking.Data.Repository;

public class AccountRepository : IAccountRepository
{
	private readonly BankingDbContext _ctx;

	public AccountRepository(BankingDbContext context)
	{
		_ctx = context;
	}

	public IEnumerable<Account> GetAccounts()
	{
		return _ctx.Accounts;
	}
}
