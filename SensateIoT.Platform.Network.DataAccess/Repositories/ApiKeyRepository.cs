/*
 * API key data access implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Threading.Tasks;
using System.Data;
using System.Threading;

using Npgsql;
using NpgsqlTypes;

using SensateIoT.Platform.Network.Data.Enums;
using SensateIoT.Platform.Network.Data.Models;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.DataAccess.Extensions;

using CommandType = System.Data.CommandType;

namespace SensateIoT.Platform.Network.DataAccess.Repositories
{
	public class ApiKeyRepository : IApiKeyRepository
	{
		private readonly IAuthorizationDbContext m_ctx;

		private const string NetworkApi_GetApiKeyByKey = "networkapi_selectapikeybykey";
		private const string NetworkApi_IncrementRequestCount = "networkapi_incrementrequestcount";

		public ApiKeyRepository(IAuthorizationDbContext ctx)
		{
			this.m_ctx = ctx;
		}

		public async Task<ApiKey> GetAsync(string key, CancellationToken ct = default)
		{
			ApiKey apikey;

			await using var cmd = this.m_ctx.Connection.CreateCommand();
			if(cmd.Connection.State != ConnectionState.Open) {
				await cmd.Connection.OpenAsync(ct).ConfigureAwait(false);
			}

			cmd.CommandType = CommandType.StoredProcedure;
			cmd.CommandText = NetworkApi_GetApiKeyByKey;

			var param = new NpgsqlParameter("key", NpgsqlDbType.Text) { Value = key };
			cmd.Parameters.Add(param);

			await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

			if(!reader.HasRows) {
				return null;
			}

			await reader.ReadAsync(ct).ConfigureAwait(false);

			apikey = new ApiKey {
				Key = key,
				UserId = reader.GetGuid(0),
				Revoked = reader.GetBoolean(1),
				Type = (ApiKeyType)reader.GetInt32(2),
				ReadOnly = reader.GetBoolean(3)
			};

			return apikey;
		}

		public async Task IncrementRequestCountAsync(string key, CancellationToken ct = default)
		{
			using var builder = StoredProcedureBuilder.Create(this.m_ctx.Connection);

			builder.WithParameter("key", key, NpgsqlDbType.Text);
			builder.WithFunction(NetworkApi_IncrementRequestCount);

			var reader = await builder.ExecuteAsync(ct).ConfigureAwait(false);
			await reader.DisposeAsync().ConfigureAwait(false);
		}
	}
}
