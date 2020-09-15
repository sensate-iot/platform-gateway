/*
 * Program startup.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SensateService.Common.Config.Config;
using SensateService.Common.IdentityData.Models;
using SensateService.Infrastructure.Sql;
using SensateService.Init;
using SensateService.Processing.StorageClient.Mqtt;

namespace SensateService.Processing.StorageClient.Application
{
	public class Startup
	{
		private readonly IConfiguration Configuration;

		public Startup(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		private static bool IsDevelopment()
		{
#if DEBUG
			var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
			return env == "Development";
#else
			return false;
#endif

		}

		public void ConfigureServices(IServiceCollection services)
		{
			var mqtt = new MqttConfig();
			var db = new DatabaseConfig();
			var cache = new CacheConfig();

			this.Configuration.GetSection("Mqtt").Bind(mqtt);
			this.Configuration.GetSection("Database").Bind(db);
			this.Configuration.GetSection("Cache").Bind(cache);

			var publicmqtt = mqtt.PublicBroker;
			var privatemqtt = mqtt.InternalBroker;

			services.AddPostgres(db.PgSQL.ConnectionString);

			services.AddIdentity<SensateUser, SensateRole>()
				.AddEntityFrameworkStores<SensateSqlContext>()
				.AddDefaultTokenProviders();
			services.AddDataProtection()
				.PersistKeysToDbContext<SensateSqlContext>()
				.DisableAutomaticKeyGeneration();

			services.AddLogging(builder => { builder.AddConfiguration(this.Configuration.GetSection("Logging")); });

			if(cache.Enabled) {
				services.AddCacheStrategy(cache, db);
			}

			services.AddDocumentStore(db.MongoDB.ConnectionString, db.MongoDB.DatabaseName, db.MongoDB.MaxConnections);
			services.AddDocumentRepositories(cache.Enabled);
			services.AddSqlRepositories(cache.Enabled);
			services.AddMeasurementStorage(cache);
			services.AddMessageStorage();
			services.AddHashAlgorihms();

			services.AddMqttService(options => {
				options.Ssl = publicmqtt.Ssl;
				options.Host = publicmqtt.Host;
				options.Port = publicmqtt.Port;
				options.Username = publicmqtt.Username;
				options.Password = publicmqtt.Password;
				options.Id = Guid.NewGuid().ToString();
				options.TopicShare = "$share/sensate-storage/";
			});

			services.AddInternalMqttService(options => {
				options.Ssl = privatemqtt.Ssl;
				options.Host = privatemqtt.Host;
				options.Port = privatemqtt.Port;
				options.Username = privatemqtt.Username;
				options.Password = privatemqtt.Password;
				options.Id = Guid.NewGuid().ToString();
				options.InternalBulkMeasurementTopic = privatemqtt.InternalBulkMeasurementTopic;
				options.AuthorizedBulkMeasurementTopic = privatemqtt.AuthorizedBulkMeasurementTopic;
				options.AuthorizedBulkMessageTopic = privatemqtt.AuthorizedBulkMessageTopic;
				options.InternalBulkMessageTopic = privatemqtt.InternalBulkMessageTopic;
			});

			services.AddSingleton<IHostedService, MqttPublishHandler>();

			services.AddLogging(builder => {
				builder.AddConfiguration(this.Configuration.GetSection("Logging"));

				if(IsDevelopment()) {
					builder.AddDebug();
				}

				builder.AddConsole();
			});

			services.AddMqttHandlers();
		}

		public void Configure(IServiceProvider provider)
		{
			var mqtt = new MqttConfig();

			this.Configuration.GetSection("Mqtt").Bind(mqtt);
			var @private = mqtt.InternalBroker;

			provider.MapMqttTopic<MqttBulkMeasurementHandler>(@private.AuthorizedBulkMeasurementTopic);
			provider.MapMqttTopic<MqttBulkMessageHandler>(@private.AuthorizedBulkMessageTopic);
		}
	}
}
