namespace SensateIoT.Platform.Network.GatewayAPI.DTO
{
	public class JsonGatewayRequest<TValue> where TValue : class
	{
		public string Json { get; set; }
		public GenericGatewayRequest<TValue> RequestData;
	}
}
