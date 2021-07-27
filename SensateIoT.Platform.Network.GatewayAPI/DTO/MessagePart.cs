using System;
using Newtonsoft.Json;
using SensateIoT.Platform.Network.Data.Abstract;

namespace SensateIoT.Platform.Network.GatewayAPI.DTO
{
	public class MessagePart
	{
		public decimal Longitude { get; set; }
		public decimal Latitude { get; set; }
		public DateTime Timestamp { get; set; }
		[JsonRequired]
		public string Data { get; set; }
		public MessageEncoding Encoding { get; set; }
	}
}
