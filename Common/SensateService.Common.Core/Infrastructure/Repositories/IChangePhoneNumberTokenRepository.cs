﻿/*
 * Change email token repository interface.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System.Threading;
using System.Threading.Tasks;

using SensateService.Common.Data.Models;
using SensateService.Common.IdentityData.Models;

namespace SensateService.Infrastructure.Repositories
{
	public interface IChangePhoneNumberTokenRepository
	{
		Task CreateAsync(ChangePhoneNumberToken token, CancellationToken ct = default(CancellationToken));
		Task<string> CreateAsync(SensateUser user, string token, string phonenumber);
		ChangePhoneNumberToken GetById(string id);
		Task<ChangePhoneNumberToken> GetLatest(SensateUser user);
	}
}