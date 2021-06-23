﻿/*
 * Control message repository implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;

using SensateIoT.Platform.Network.Data.Models;

namespace SensateIoT.Platform.Network.DataAccess.Abstract
{
	public interface IControlMessageRepository
	{
		Task DeleteBySensorAsync(ObjectId sensor, CancellationToken ct = default);
		Task DeleteBySensorIds(IEnumerable<ObjectId> sensorIds, CancellationToken ct = default);
	}
}
