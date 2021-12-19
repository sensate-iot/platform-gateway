/*
 * API application startup.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using SensateIoT.Platform.Network.Common.Adapters;
using SensateIoT.Platform.Network.Common.Adapters.Abstract;
using SensateIoT.Platform.Network.Common.Init;
using SensateIoT.Platform.Network.Common.Services.Metrics;
using SensateIoT.Platform.Network.Common.Services.Processing;
using SensateIoT.Platform.Network.Common.Settings;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.DataAccess.Repositories;
using SensateIoT.Platform.Network.GatewayAPI.Abstract;
using SensateIoT.Platform.Network.GatewayAPI.Authorization;
using SensateIoT.Platform.Network.GatewayAPI.Config;
using SensateIoT.Platform.Network.GatewayAPI.Extensions;
using SensateIoT.Platform.Network.GatewayAPI.Filters;
using SensateIoT.Platform.Network.GatewayAPI.Middleware;
using SensateIoT.Platform.Network.GatewayAPI.Services;

using IAuthorizationService = SensateIoT.Platform.Network.Common.Services.Processing.IAuthorizationService;

namespace SensateIoT.Platform.Network.GatewayAPI.Application
{
	public class Startup
	{
		private readonly IConfiguration m_configuration;

		public Startup(IConfiguration configuration)
		{
			this.m_configuration = configuration;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			var db = new DatabaseConfig();

			this.m_configuration.GetSection("Database").Bind(db);
			var proxyLevel = this.m_configuration.GetValue<int>("System:ProxyLevel");

			services.AddDocumentStore(db.MongoDB.ConnectionString, db.MongoDB.DatabaseName, db.MongoDB.MaxConnections);
			services.AddConnectionStrings(db.Networking.ConnectionString, db.SensateIoT.ConnectionString);
			services.AddAuthorizationContext();
			services.AddNetworkingContext();
			services.AddCors();

			services.Configure<ApiBehaviorOptions>(o => {
				o.SuppressModelStateInvalidFilter = true;
				o.SuppressMapClientErrors = true;
			});

			services.Configure<RouterConfig>(this.m_configuration.GetSection("Router"));
			services.Configure<BlobOptions>(this.m_configuration.GetSection("Storage"));
			services.Configure<MetricsOptions>(this.m_configuration.GetSection("HttpServer:Metrics"));

			services.AddScoped<ISensorRepository, SensorRepository>();
			services.AddScoped<IAccountRepository, AccountRepository>();
			services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
			services.AddScoped<IBlobRepository, BlobRepository>();

			services.AddSingleton<IBlobService, FilesystemBlobService>();
			services.AddSingleton<IRouterClient, RouterClient>();

			services.AddSingleton<IBulkMeasurementAuthorizationService, BulkMeasurementAuthorizationService>();
			services.AddSingleton<IBulkMessageAuthorizationService, BulkMessageAuthorizationService>();
			services.AddSingleton<IAuthorizationService, AuthorizationService>();
			services.AddSingleton<IMeasurementAuthorizationService, MeasurementAuthorizationService>();
			services.AddSingleton<IMessageAuthorizationService, MessageAuthorizationService>();
			services.AddSingleton<IHashAlgorithm, SHA256Algorithm>();
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

			services.AddHostedService<BatchRoutingService>();
			services.AddHostedService<MetricsService>();

			services.AddSwaggerGen(c => {
				c.SwaggerDoc("v1", new OpenApiInfo {
					Title = "Sensate IoT Gateway API - Version 1",
					Version = "v1"
				});

				c.SwaggerDoc("v2", new OpenApiInfo {
					Title = "Sensate IoT Gateway API - Version 2",
					Version = "v2"
				});

				c.SchemaFilter<ObjectIdSchemaFilter>();
				c.OperationFilter<ObjectIdOperationFilter>();

				c.AddSecurityDefinition("X-ApiKey", new OpenApiSecurityScheme {
					In = ParameterLocation.Header,
					Name = "X-ApiKey",
					Type = SecuritySchemeType.ApiKey,
					Description = "API key needed to access the endpoints."
				});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Name = "X-ApiKey",
							Type = SecuritySchemeType.ApiKey,
							In = ParameterLocation.Header,
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "X-ApiKey"
							},
						},
						Array.Empty<string>()
					}
				});
			});

			services.AddReverseProxy(proxyLevel);
			services.AddRouting();
			services.AddApiVersioning(o => {
				o.ReportApiVersions = true;
				o.AssumeDefaultVersionWhenUnspecified = true;
				o.DefaultApiVersion = new ApiVersion(1, 0);
			});
			services.AddVersionedApiExplorer(options => {
				options.GroupNameFormat = "'v'V";
				options.SubstituteApiVersionInUrl = true;
			});
			services.AddControllers().AddNewtonsoftJson();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseForwardedHeaders();
			app.UseRouting();

			app.UseCors(p => {
				p.SetIsOriginAllowed(host => true)
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
			});

			app.UseSwagger(c => {
				c.RouteTemplate = "gateway/swagger/{documentName}/swagger.json";

				c.PreSerializeFilters.Add((swagger, httpReq) => {
					swagger.Servers = new List<OpenApiServer> {
						new OpenApiServer { Url = $"http://{httpReq.Host.Value}" },
						new OpenApiServer { Url = $"https://{httpReq.Host.Value}" }
					};
				});
			});

			app.UseSwaggerUI(c => {
				c.SwaggerEndpoint("/gateway/swagger/v1/swagger.json", "Sensate IoT Gateway API v1");
				c.SwaggerEndpoint("/gateway/swagger/v2/swagger.json", "Sensate IoT Gateway API v2");
				c.RoutePrefix = "gateway/swagger";
			});

			if(env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}

			app.UseCors(p => {
				p.SetIsOriginAllowed(host => true)
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
			});

			app.UseMiddleware<ErrorLoggingMiddleware>();
			app.UseMiddleware<ApiKeyValidationMiddleware>();
			app.UseMiddleware<RequestLoggingMiddleware>();
			app.UseMiddleware<JsonErrorHandlerMiddleware>();

			app.UseEndpoints(endpoints => {
				endpoints.MapControllers();
			});
		}
	}
}
