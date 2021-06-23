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

using SensateIoT.Platform.Network.Data.Models;

namespace SensateIoT.Platform.Network.DataAccess.Abstract
{
	public interface ISensorRepository
	{
		Task<IEnumerable<Sensor>> GetAsync(Guid userId, int skip = 0, int limit = 0);
		Task<Sensor> GetAsync(ObjectId id, CancellationToken ct = default);
		Task<IEnumerable<Sensor>> GetAsync(IEnumerable<string> ids);
	}
}
