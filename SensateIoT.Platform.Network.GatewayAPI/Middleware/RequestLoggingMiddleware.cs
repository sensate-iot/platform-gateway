/*
 * Request logging middleware.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Diagnostics;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SensateIoT.Platform.Network.GatewayAPI.Middleware
{
	[UsedImplicitly]
	public class RequestLoggingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<RequestLoggingMiddleware> m_logger;

		public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
		{
			this._next = next;
			this.m_logger = logger;
		}

		[UsedImplicitly]
		public async Task Invoke(HttpContext ctx)
		{
			var sw = Stopwatch.StartNew();

			await this._next(ctx).ConfigureAwait(false);

			sw.Stop();

			this.m_logger.LogInformation(
				"{method} {path} from {ip} resulted in {response} ({duration}ms)",
				ctx.Request.Method,
				ctx.Request.Path,
				ctx.Request.HttpContext.Connection.RemoteIpAddress,
				ctx.Response.StatusCode,
				sw.ElapsedMilliseconds
			);
		}
	}
}
