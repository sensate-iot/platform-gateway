using System.Threading.Tasks;
using SensateIoT.Platform.Network.GatewayAPI.DTO;

namespace SensateIoT.Platform.Network.GatewayAPI.Abstract
{
	public interface IBulkMeasurementAuthorizationService
	{
		void AddMessage(JsonGatewayRequest<MeasurementPart> data);
		Task<int> ProcessAsync();
	}
}
