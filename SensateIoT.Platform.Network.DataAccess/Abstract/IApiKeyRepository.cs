﻿/*
 * API data access repository.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Threading;
using System.Threading.Tasks;

using SensateIoT.Platform.Network.Data.Models;

namespace SensateIoT.Platform.Network.DataAccess.Abstract
{
	public interface IApiKeyRepository
	{
		Task<ApiKey> GetAsync(string key, CancellationToken ct = default);
		Task IncrementRequestCountAsync(string key, CancellationToken ct = default);
	}
}
