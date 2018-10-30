/*
 * Extension methods to initialise a new websocket service.
 *
 * @author Michel Megens
 * @email  dev@bietje.net
 */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using SensateService.ApiCore.Middleware;
using SensateService.Infrastructure;
using SensateService.Infrastructure.Repositories;
using SensateService.Middleware;

namespace SensateService.ApiCore.Init
{
	public static class WebSocketServiceInitExtensions
	{
		public static IApplicationBuilder MapWebSocketService(
			this IApplicationBuilder application,
			PathString path,
			WebSocketHandler handler
		)
		{
			return application.Map(path, (_app) => _app.UseMiddleware<WebSocketService>(handler));
		}

		public static IServiceCollection AddWebSocketHandler<T>(this IServiceCollection sc) where T : WebSocketHandler
		{
			sc.AddSingleton<T>();
			return sc;
		}
	}
}
