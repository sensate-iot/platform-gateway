/*
 * Route messages in batches to the message router.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using SensateIoT.Platform.Network.Common.Services.Background;
using SensateIoT.Platform.Network.GatewayAPI.Abstract;

namespace SensateIoT.Platform.Network.GatewayAPI.Services
{
	public class BatchRoutingService : TimedBackgroundService
	{
		private readonly IMeasurementAuthorizationService m_measurements;
		private readonly IMessageAuthorizationService m_messages;
		private readonly IBulkMeasurementAuthorizationService m_bulkMeasurements;
		private readonly IBulkMessageAuthorizationService m_bulkMessages;
		private readonly ILogger<BatchRoutingService> m_logger;

		public BatchRoutingService(IMeasurementAuthorizationService measurements,
								   IMessageAuthorizationService messages,
								   IBulkMeasurementAuthorizationService bulkMeasurements,
								   IBulkMessageAuthorizationService bulkMessages,
								   ILogger<BatchRoutingService> logger) :
			base(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500), logger)
		{
			this.m_measurements = measurements;
			this.m_messages = messages;
			this.m_logger = logger;
			this.m_bulkMeasurements = bulkMeasurements;
			this.m_bulkMessages = bulkMessages;
		}

		protected override async Task ExecuteAsync(CancellationToken token)
		{
			try {
				await Task.WhenAll(this.m_measurements.ProcessAsync(),
				                   this.m_bulkMessages.ProcessAsync(),
				                   this.m_bulkMeasurements.ProcessAsync(),
				                   this.m_messages.ProcessAsync())
					.ConfigureAwait(false);
			} catch(RpcException ex) {
				this.m_logger.LogError(ex, "Unable to route messages");
			} catch(Exception ex) {
				// Only catch to log.
				this.m_logger.LogCritical(ex, "Unknown routing error");
				Environment.Exit(-1);
			}
		}
	}
}
