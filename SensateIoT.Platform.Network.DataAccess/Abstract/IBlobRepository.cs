/*
 * Blobs data access repository.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Threading;
using System.Threading.Tasks;

using SensateIoT.Platform.Network.Data.Models;

namespace SensateIoT.Platform.Network.DataAccess.Abstract
{
	public interface IBlobRepository
	{
		Task<Blob> CreateAsync(Blob blob, CancellationToken ct = default);
	}
}
