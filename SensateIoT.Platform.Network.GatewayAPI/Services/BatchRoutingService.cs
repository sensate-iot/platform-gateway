/*
 * Route messages in batches to the message router.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SensateIoT.Platform.Network.Common.Services.Background;
using SensateIoT.Platform.Network.GatewayAPI.Abstract;

namespace SensateIoT.Platform.Network.GatewayAPI.Services
{
	public class BatchRoutingService : TimedBackgroundService
	{
		private readonly IMeasurementAuthorizationService m_measurements;
		private readonly IMessageAuthorizationService m_messages;
		private readonly ILogger<BatchRoutingService> m_logger;

		public BatchRoutingService(IMeasurementAuthorizationService measurements,
								   IMessageAuthorizationService messages,
								   ILogger<BatchRoutingService> logger) :
			base(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500), logger)
		{
			this.m_measurements = measurements;
			this.m_messages = messages;
			this.m_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken token)
		{
			try {
				await Task.WhenAll(this.m_measurements.ProcessAsync(), this.m_messages.ProcessAsync())
					.ConfigureAwait(false);
			} catch(Exception ex) {
				// Only catch to log.
				this.m_logger.LogError("Unable to write messages to the router: {message}. Trace: {trace}", ex.Message, ex.StackTrace);
				throw;
			}
		}
	}
}
