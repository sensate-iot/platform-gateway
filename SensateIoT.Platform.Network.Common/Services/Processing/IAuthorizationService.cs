/*
 * Authorization service interface.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using SensateIoT.Platform.Network.Data.DTO;

namespace SensateIoT.Platform.Network.Common.Services.Processing
{
	public interface IAuthorizationService
	{
		void SignControlMessage(ControlMessage message, string json);
	}
}
