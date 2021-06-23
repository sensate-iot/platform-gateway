/*
 * Account repository implementation.
 * 
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Npgsql;
using NpgsqlTypes;

using SensateIoT.Platform.Network.Data.Models;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.DataAccess.Extensions;

namespace SensateIoT.Platform.Network.DataAccess.Repositories
{
	public class AccountRepository : IAccountRepository
	{
		private const string NetworkApi_GetAccountByID = "networkapi_selectuserbyid";

		private readonly IAuthorizationDbContext m_authorization;

		public AccountRepository(IAuthorizationDbContext authorization)
		{
			this.m_authorization = authorization;
		}

		public async Task<User> GetAccountAsync(Guid accountGuid, CancellationToken ct = default)
		{
			await using var cmd = this.m_authorization.Connection.CreateCommand();
			if(cmd.Connection.State != ConnectionState.Open) {
				await cmd.Connection.OpenAsync(ct).ConfigureAwait(false);
			}

			cmd.CommandType = CommandType.StoredProcedure;
			cmd.CommandText = NetworkApi_GetAccountByID;

			var param = new NpgsqlParameter("userid", NpgsqlDbType.Uuid) { Value = accountGuid };
			cmd.Parameters.Add(param);

			await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
			return await GetAccountAsync(reader, ct).ConfigureAwait(false);
		}

		private static async Task<User> GetAccountAsync(DbDataReader reader, CancellationToken ct = default)
		{
			User user;

			if(!reader.HasRows) {
				return null;
			}

			await reader.ReadAsync(ct).ConfigureAwait(false);

			user = new User {
				ID = reader.GetGuid(0),
				FirstName = reader.SafeGetString(1),
				LastName = reader.SafeGetString(2),
				Email = reader.GetString(3),
				RegisteredAt = reader.GetDateTime(4),
				PhoneNumber = reader.SafeGetString(5),
				BillingLockout = reader.GetBoolean(6),
				UserRoles = new List<string> { reader.GetString(7) }
			};

			while(await reader.ReadAsync(ct)) {
				user.UserRoles.Add(reader.GetString(7));
			}

			return user;

		}
	}
}
