using System.Threading.Tasks;
using SensateIoT.Platform.Network.GatewayAPI.DTO;

namespace SensateIoT.Platform.Network.GatewayAPI.Abstract
{
	public interface IBulkMessageAuthorizationService
	{
		void AddMessage(JsonGatewayRequest<MessagePart> data);
		Task<int> ProcessAsync();
	}
}
