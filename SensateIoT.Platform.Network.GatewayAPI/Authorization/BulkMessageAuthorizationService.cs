/*
 * Message authorization handler.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SensateIoT.Platform.Network.Contracts.DTO;
using SensateIoT.Platform.Network.Data.Models;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.GatewayAPI.Abstract;
using SensateIoT.Platform.Network.GatewayAPI.DTO;

namespace SensateIoT.Platform.Network.GatewayAPI.Authorization
{
	public class BulkMessageAuthorizationService : AbstractAuthorizationHandler<JsonGatewayRequest<MessagePart>>, IBulkMessageAuthorizationService
	{
		private readonly IHashAlgorithm m_algo;
		private readonly IRouterClient m_router;
		private readonly ILogger<BulkMessageAuthorizationService> m_logger;
		private readonly IServiceProvider m_provider;

		public BulkMessageAuthorizationService(IServiceProvider provider, IHashAlgorithm algo, IRouterClient client, ILogger<BulkMessageAuthorizationService> logger)
		{
			this.m_logger = logger;
			this.m_algo = algo;
			this.m_provider = provider;
			this.m_router = client;
		}

		public override async Task<int> ProcessAsync()
		{
			List<JsonGatewayRequest<MessagePart>> messages;

			this.m_lock.Lock();

			try {
				var newList = new List<JsonGatewayRequest<MessagePart>>();
				messages = this.m_messages;
				this.m_messages = newList;
			} finally {
				this.m_lock.Unlock();
			}

			if(messages == null || messages.Count <= 0) {
				return 0;
			}

			messages = messages.OrderBy(m => m.RequestData.SensorId).ToList();
			return await this.WriteToRouterAsync(messages).ConfigureAwait(false);
		}

		private async Task<int> WriteToRouterAsync(IEnumerable<JsonGatewayRequest<MessagePart>> measurements)
		{
			var data = await this.BuildMessageList(measurements).ConfigureAwait(false);
			var remaining = data.Count;
			var index = 0;
			var tasks = new List<Task>();

			if(remaining <= 0) {
				return 0;
			}

			while(remaining > 0) {
				var batch = Math.Min(PartitionSize, remaining);

				tasks.Add(this.SendBatchToRouterAsync(index, remaining, data));
				index += batch;
				remaining -= batch;
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
			return data.Count;
		}

		private async Task SendBatchToRouterAsync(int start, int count, IList<TextMessage> messages)
		{
			var data = new TextMessageData();

			for(var idx = start; idx < start + count; idx++) {
				var message = messages[idx];
				data.Messages.Add(message);
			}

			var result = await this.m_router.RouteAsync(data, default).ConfigureAwait(false);
			this.m_logger.LogInformation("Sent {inputCount} messages to the router. {outputCount} " +
										 "messages have been accepted. Router response ID: {responseID}.",
										 data.Messages.Count, result.Count, new Guid(result.ResponseID.Span));
		}

		private async Task<IList<TextMessage>> BuildMessageList(IEnumerable<JsonGatewayRequest<MessagePart>> messages)
		{
			Sensor sensor = null;
			var data = new List<TextMessage>();

			using var scope = this.m_provider.CreateScope();
			var repo = scope.ServiceProvider.GetRequiredService<ISensorRepository>();

			foreach(var message in messages) {
				try {
					if(sensor == null || sensor.InternalId != message.RequestData.SensorId) {
						sensor = await repo.GetAsync(message.RequestData.SensorId).ConfigureAwait(false);
					}

					if(sensor == null || !this.AuthorizeMessage(message, sensor)) {
						continue;
					}

					ConvertToContractMessage(message, sensor, data);
				} catch(Exception ex) {
					this.m_logger.LogInformation(ex, "Unable to process message: {message}", ex.InnerException?.Message);
				}
			}

			return data;
		}

		private static void ConvertToContractMessage(JsonGatewayRequest<MessagePart> message, Sensor sensor, ICollection<TextMessage> data)
		{
			foreach(var value in message.RequestData.Values) {
				if(value.Timestamp == DateTime.MinValue) {
					value.Timestamp = DateTime.UtcNow;
				}

				var m = new TextMessage {
					SensorID = sensor.InternalId.ToString(),
					Latitude = decimal.ToDouble(value.Latitude),
					Longitude = decimal.ToDouble(value.Longitude),
					Timestamp = Timestamp.FromDateTime(value.Timestamp),
					Data = value.Data,
					Encoding = (int) value.Encoding
				};

				data.Add(m);
			}
		}

		protected override bool AuthorizeMessage(JsonGatewayRequest<MessagePart> message, Sensor sensor)
		{
			var match = this.m_algo.GetMatchRegex();
			var search = this.m_algo.GetSearchRegex();

			if(match.IsMatch(message.RequestData.Secret)) {
				var length = message.RequestData.Secret.Length - SecretSubStringOffset;
				var hash = HexToByteArray(message.RequestData.Secret.Substring(SecretSubStringStart, length));
				var json = search.Replace(message.Json, sensor.Secret, 1);
				var binary = Encoding.ASCII.GetBytes(json);
				var computed = this.m_algo.ComputeHash(binary);

				if(!CompareHashes(computed, hash)) {
					return false;
				}
			} else {
				return message.RequestData.Secret == sensor.Secret;
			}

			return true;
		}
	}
}
