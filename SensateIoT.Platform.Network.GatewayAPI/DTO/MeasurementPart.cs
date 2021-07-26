/*
 * Measurement DTO object.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

using DataPointMap = System.Collections.Generic.IDictionary<string, SensateIoT.Platform.Network.Data.Models.DataPoint>;

namespace SensateIoT.Platform.Network.GatewayAPI.DTO
{
	[UsedImplicitly]
	public class MeasurementPart
	{
		public decimal Longitude { get; set; }
		public decimal Latitude { get; set; }
		public DateTime Timestamp { get; set; }
		[JsonRequired]
		public DataPointMap Data { get; set; }
	}
}