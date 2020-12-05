﻿/*
 * TriggerHandler entry point.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SensateIoT.Platform.Network.TriggerService.Application
{
	public class Program
	{
		public static string GetAppSettings()
		{
			return Environment.GetEnvironmentVariable("SENSATE_TRIGGERHANDLER_APPSETTINGS") ?? "appsettings.Development.json";
		}

		public static IHost CreateHost(string[] args)
		{
			Startup starter = null;

			var wh = Host.CreateDefaultBuilder(args)
				.UseContentRoot(Directory.GetCurrentDirectory())
				.ConfigureAppConfiguration((hostingContext, config) => {
					config.AddJsonFile(GetAppSettings(), optional: false, reloadOnChange: true);
					config.AddEnvironmentVariables();
				})
				.ConfigureLogging((hostingContext, logging) => {
					logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
					logging.AddConsole();

					if(hostingContext.HostingEnvironment.IsDevelopment())
						logging.AddDebug();
				})
				.ConfigureServices((ctx, services) => {
					starter = new Startup(ctx.Configuration);
					starter.ConfigureServices(services);
				});

			var host = wh.Build();
			var provider = host.Services;

			if(starter == null)
				return null;

			starter.Configure(provider);
			return host;
		}

		public static void Main(string[] args)
		{
			Console.WriteLine($"Starting TriggerService using {Assembly.GetExecutingAssembly().GetName().Version}");

			var program = new AppHost(CreateHost(args));
			program.Run();
		}
	}
}