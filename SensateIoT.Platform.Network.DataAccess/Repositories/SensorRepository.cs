/*
 * Data repository for sensor information.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.DataAccess.Contexts;

using Sensor = SensateIoT.Platform.Network.Data.Models.Sensor;

namespace SensateIoT.Platform.Network.DataAccess.Repositories
{
	public class SensorRepository : ISensorRepository
	{
		private readonly IMongoCollection<Sensor> m_sensors;

		public SensorRepository(MongoDBContext ctx)
		{
			this.m_sensors = ctx.Sensors;
		}

		public async Task<IEnumerable<Sensor>> GetAsync(Guid userId, int skip = 0, int limit = 0)
		{
			FilterDefinition<Sensor> filter;
			var builder = Builders<Sensor>.Filter;
			var id = userId.ToString();

			filter = builder.Where(s => s.Owner == id);
			var sensors = this.m_sensors.Aggregate().Match(filter);

			if(sensors == null) {
				return null;
			}

			if(skip > 0) {
				sensors = sensors.Skip(skip);
			}

			if(limit > 0) {
				sensors = sensors.Limit(limit);
			}

			return await sensors.ToListAsync().ConfigureAwait(false);
		}

		public async Task<Sensor> GetAsync(ObjectId id, CancellationToken ct)
		{
			var filter = Builders<Sensor>.Filter.Where(x => x.InternalId == id);
			var result = await this.m_sensors.FindAsync(filter).ConfigureAwait(false);

			if(result == null) {
				return null;
			}

			return await result.FirstOrDefaultAsync(ct).ConfigureAwait(false);
		}

		public async Task<IEnumerable<Sensor>> GetAsync(IEnumerable<string> ids)
		{
			FilterDefinition<Sensor> filter;
			var builder = Builders<Sensor>.Filter;
			var idlist = new List<ObjectId>();

			foreach(var id in ids) {
				if(!ObjectId.TryParse(id, out var parsedId)) {
					continue;
				}

				idlist.Add(parsedId);
			}

			filter = builder.In(x => x.InternalId, idlist);
			var raw = await this.m_sensors.FindAsync(filter).ConfigureAwait(false);
			return raw.ToList();
		}
	}
}
