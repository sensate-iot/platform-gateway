using System.Collections.Generic;
using MongoDB.Bson;
using Newtonsoft.Json;
using SensateIoT.Platform.Network.Data.Converters;

namespace SensateIoT.Platform.Network.GatewayAPI.DTO
{
	public class GenericGatewayRequest<TValue> where TValue : class
	{
		[JsonRequired, JsonConverter(typeof(ObjectIdJsonConverter))]
		public ObjectId SensorId { get; set; }
		[JsonRequired]
		public string Secret { get; set; }
		[JsonRequired]
		public IEnumerable<TValue> Values { get; set; }
	}
}
