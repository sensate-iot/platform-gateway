﻿/*
 * Sensor HTTP controller.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;

using SensateService.ApiCore.Attributes;
using SensateService.ApiCore.Controllers;
using SensateService.Enums;
using SensateService.Helpers;
using SensateService.Infrastructure.Repositories;
using SensateService.Models;
using SensateService.Models.Json.Out;
using SensateService.NetworkApi.Models;
using SensateService.Services;

namespace SensateService.NetworkApi.Controllers
{
	[Produces("application/json")]
	[Route("[controller]")]
	public class SensorsController : AbstractDataController
	{
		private readonly ILogger<SensorsController> m_logger;
		private readonly IApiKeyRepository m_apiKeys;
		private readonly IMeasurementRepository m_measurements;
		private readonly IMessageRepository m_messages;
		private readonly ITriggerRepository m_triggers;
		private readonly ISensorStatisticsRepository m_stats;
		private readonly IUserRepository m_users;
		private readonly ISensorService m_sensorService;

		public SensorsController(IHttpContextAccessor ctx, ISensorRepository sensors, ILogger<SensorsController> logger,
			IMeasurementRepository measurements,
			ITriggerRepository triggers,
			IMessageRepository messages,
			IUserRepository users,
			ISensorLinkRepository links,
			ISensorStatisticsRepository stats,
			ISensorService sensorService,
			IApiKeyRepository keys) : base(ctx, sensors, links)
		{
			this.m_logger = logger;
			this.m_apiKeys = keys;
			this.m_measurements = measurements;
			this.m_triggers = triggers;
			this.m_messages = messages;
			this.m_stats = stats;
			this.m_users = users;
			this.m_sensorService = sensorService;
		}

		[HttpGet]
		[ActionName("FindSensorsByName")]
		[ProducesResponseType(typeof(IEnumerable<Sensor>), 200)]
		public async Task<IActionResult> Index([FromQuery] string name)
		{
			IEnumerable<Sensor> sensors;

			if(string.IsNullOrEmpty(name)) {
				sensors = await this.m_sensorService.GetSensorsAsync(this.CurrentUser).AwaitBackground();
			} else {
				sensors = await this.m_sensorService.GetSensorsAsync(this.CurrentUser, name).AwaitBackground();
			}
				
			return this.Ok(sensors);
		}

		[HttpPost]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status422UnprocessableEntity)]
		public async Task<IActionResult> Post([FromBody] Sensor sensor)
		{
			sensor.Owner = this.CurrentUser.Id;

			try {
				await this.m_apiKeys.CreateSensorKey(new SensateApiKey(), sensor).AwaitBackground();
				await this.m_sensors.CreateAsync(sensor).AwaitBackground();
			} catch(Exception ex) {
				this.m_logger.LogInformation($"Unable to create sensor: {ex.Message}");

				return this.BadRequest(new Status {
					Message = "Unable to save sensor.",
					ErrorCode = ReplyCode.BadInput
				});
			}

			return this.CreatedAtAction(nameof(Get), new {Id = sensor.InternalId}, sensor);
		}

		[HttpPost("link")]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status422UnprocessableEntity)]
		public async Task<IActionResult> Link([FromBody] SensorLink link)
		{
			var status = new Status();

			try {
				var sensor = await this.m_sensors.GetAsync(link.SensorId).AwaitBackground();
				var user = await this.m_users.GetByEmailAsync(link.UserId).AwaitBackground();

				if(!this.AuthenticateUserForSensor(sensor, false)) {
					return this.Forbid();
				}

				if(user == null) {
					status.Message = "Target user not known!";
					status.ErrorCode = ReplyCode.BadInput;

					return this.NotFound(status);
				}

				if(sensor == null) {
					status.Message = "Target sensor not known!";
					status.ErrorCode = ReplyCode.BadInput;

					return this.NotFound(status);
				}

				link.UserId = user.Id;
				await this.m_links.CreateAsync(link).AwaitBackground();
			} catch(Exception ex) {
				this.m_logger.LogInformation($"Unable to link sensor: {ex.Message}");
				this.m_logger.LogDebug(ex.StackTrace);

				return this.BadRequest(new Status {
					Message = "Unable to link sensor!",
					ErrorCode = ReplyCode.BadInput
				});
			}

			return this.NoContent();
		}

		[HttpPatch("{id}")]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status422UnprocessableEntity)]
		public async Task<IActionResult> Patch(string id, [FromBody] SensorUpdate update)
		{
			Sensor sensor;
			if(!ObjectId.TryParse(id, out _)) {
				return this.UnprocessableEntity(new Status {
					Message = "Unable to parse ID",
					ErrorCode = ReplyCode.BadInput
				});
			}


			try {
				sensor = await this.m_sensors.GetAsync(id).AwaitBackground();

				if(!this.AuthenticateUserForSensor(sensor, false)) {
					return this.Forbid();
				}

				if(sensor == null) {
					return this.NotFound();
				}

				if(!string.IsNullOrEmpty(update.Name)) {
					sensor.Name = update.Name;
				}

				if(!string.IsNullOrEmpty(update.Description)) {
					sensor.Description = update.Description;
				}

				await this.m_sensors.UpdateAsync(sensor).AwaitBackground();
			} catch(Exception ex) {
				this.m_logger.LogInformation($"Unable to update sensor: {ex.Message}");

				return this.BadRequest(new Status {
					Message = "Unable to update sensor.",
					ErrorCode = ReplyCode.BadInput
				});
			}

			return this.Ok(sensor);
		}

		[HttpPatch("{id}/secret")]
		[ReadWriteApiKey]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status422UnprocessableEntity)]
		public async Task<IActionResult> PatchSecret(string id, [FromBody] SensorUpdate update)
		{
			Sensor sensor;

			if(update == null) {
				update = new SensorUpdate();
			}

			if(!ObjectId.TryParse(id, out _)) {
				return this.UnprocessableEntity(new Status {
					Message = "Unable to parse ID",
					ErrorCode = ReplyCode.BadInput
				});
			}

			try {
				sensor = await this.m_sensors.GetAsync(id).AwaitBackground();

				if(!this.AuthenticateUserForSensor(sensor, false)) {
					return this.Forbid();
				}

				if(sensor == null) {
					return this.NotFound();
				}

				var key = await this.m_apiKeys.GetByKeyAsync(update.Secret).AwaitBackground();
				var old = await this.m_apiKeys.GetByKeyAsync(sensor.Secret).AwaitBackground();

				if(key != null) {
					return this.BadRequest(new Status {
						Message = "Unable to update sensor.",
						ErrorCode = ReplyCode.BadInput
					});
				}

				sensor.Secret = string.IsNullOrEmpty(update.Secret) ? this.m_apiKeys.GenerateApiKey() : update.Secret;

				await this.m_apiKeys.RefreshAsync(old, sensor.Secret).AwaitBackground();
				await this.m_sensors.UpdateSecretAsync(sensor, old).AwaitBackground();
			} catch(Exception ex) {
				this.m_logger.LogInformation($"Unable to update sensor: {ex.Message}");

				return this.BadRequest(new Status {
					Message = "Unable to update sensor.",
					ErrorCode = ReplyCode.BadInput
				});
			}

			return this.Ok(sensor);
		}

		[HttpDelete("{id}")]
		[ReadWriteApiKey]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status204NoContent)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Sensor), StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Delete(string id)
		{
			try {
				var sensor = await this.m_sensors.GetAsync(id).AwaitBackground();

				if(sensor == null) {
					return this.NotFound();
				}

				if(!this.AuthenticateUserForSensor(sensor, false)) {
					return this.Forbid();
				}

				var tasks = new[] {
					this.m_sensors.RemoveAsync(sensor),
					this.m_apiKeys.DeleteAsync(this.CurrentUser, sensor.Secret),
					this.m_messages.DeleteBySensorAsync(sensor),
					this.m_stats.DeleteBySensorAsync(sensor),
					this.m_measurements.DeleteBySensorAsync(sensor)
				};

				await Task.WhenAll(tasks).AwaitBackground();
				await this.m_triggers.DeleteBySensorAsync(id).AwaitBackground();
			} catch(Exception ex) {
				this.m_logger.LogInformation($"Unable to remove sensor: {ex.Message}");
				this.m_logger.LogDebug(ex.StackTrace);

				return this.BadRequest(new Status {
					Message = "Unable to delete sensor.",
					ErrorCode = ReplyCode.BadInput
				});
			}

			return this.NoContent();
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(Sensor), 200)]
		public async Task<IActionResult> Get(string id)
		{
			var sensor = await this.m_sensors.GetAsync(id).AwaitBackground();

			if(sensor == null) {
				return this.NotFound();
			}

			var linked = await this.IsLinkedSensor(id).AwaitBackground();

			if(!this.AuthenticateUserForSensor(sensor, false) && !linked) {
				return this.Forbid();
			}

			return this.Ok(sensor);
		}
	}
}
