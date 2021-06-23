/*
 * Blob repository implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Threading;
using System.Threading.Tasks;

using NpgsqlTypes;

using SensateIoT.Platform.Network.Data.Models;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.DataAccess.Extensions;

namespace SensateIoT.Platform.Network.DataAccess.Repositories
{
	public class BlobRepository : IBlobRepository
	{
		private readonly INetworkingDbContext m_ctx;

		private const string CreateBlob = "networkapi_createblob";

		public BlobRepository(INetworkingDbContext ctx)
		{
			this.m_ctx = ctx;
		}

		public async Task<Blob> CreateAsync(Blob blob, CancellationToken ct)
		{
			using var builder = StoredProcedureBuilder.Create(this.m_ctx.Connection);

			builder.WithFunction(CreateBlob);
			builder.WithParameter("sensorid", blob.SensorID, NpgsqlDbType.Varchar);
			builder.WithParameter("filename", blob.FileName, NpgsqlDbType.Text);
			builder.WithParameter("path", blob.Path, NpgsqlDbType.Text);
			builder.WithParameter("storage", (int) blob.StorageType, NpgsqlDbType.Integer);
			builder.WithParameter("filesize", blob.FileSize, NpgsqlDbType.Integer);
			await using var reader = await builder.ExecuteAsync(ct).ConfigureAwait(false);

			if(!await reader.ReadAsync(ct).ConfigureAwait(false)) {
				return null;
			}

			return new Blob {
				ID = reader.GetInt64(0),
				SensorID = reader.GetString(1),
				FileName = reader.GetString(2),
				Path = reader.GetString(3),
				StorageType = (StorageType) reader.GetInt32(4),
				Timestamp = reader.GetDateTime(5),
				FileSize = reader.GetInt32(6)
			};
		}
	}
}
