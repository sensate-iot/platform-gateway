/*
 * Message authorization interface.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Threading.Tasks;
using SensateIoT.Platform.Network.GatewayAPI.DTO;

namespace SensateIoT.Platform.Network.GatewayAPI.Abstract
{
	public interface IMessageAuthorizationService
	{
		void AddMessage(JsonMessage data);
		Task<int> ProcessAsync();
	}
}
