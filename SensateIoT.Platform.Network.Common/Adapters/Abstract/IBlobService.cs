/*
 * Blob service abstraction.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Threading;
using System.Threading.Tasks;
using SensateIoT.Platform.Network.Data.Models;

namespace SensateIoT.Platform.Network.Common.Adapters.Abstract
{
	public interface IBlobService
	{
		Task StoreAsync(Blob blob, byte[] data, CancellationToken ct = default);
	}
}