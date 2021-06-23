/*
 * Sensor aggregation service.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SensateIoT.Platform.Network.GatewayAPI.Abstract
{
	public interface ISensorService
	{
		Task DeleteAsync(Guid userId, CancellationToken ct = default);
	}
}
