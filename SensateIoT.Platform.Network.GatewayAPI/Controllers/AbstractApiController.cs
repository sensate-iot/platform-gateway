/*
 * Abstract API controller.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using SensateIoT.Platform.Network.Data.Enums;
using SensateIoT.Platform.Network.Data.Models;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.GatewayAPI.Constants;

namespace SensateIoT.Platform.Network.GatewayAPI.Controllers
{
	public class AbstractApiController : Controller
	{
		private User CurrentUser { get; }
		private ApiKey ApiKey { get; }

		protected string m_currentUserId;
		protected readonly ISensorRepository m_sensors;
		private readonly IApiKeyRepository m_keys;

		public AbstractApiController(
			IHttpContextAccessor ctx,
			ISensorRepository sensors,
			IApiKeyRepository keys
		)
		{
			if(ctx?.HttpContext == null) {
				return;
			}

			var key = ctx.HttpContext.Items["ApiKey"] as ApiKey;

			this.ApiKey = key;
			this.CurrentUser = key?.User;
			this.m_currentUserId = key?.UserId.ToString();
			this.m_sensors = sensors;
			this.m_keys = keys;
		}

		protected async Task<bool> AuthenticateUserForSensor(string sensorId, bool strict = false)
		{
			var sensor = await this.m_sensors.GetAsync(ObjectId.Parse(sensorId)).ConfigureAwait(false);

			if(sensor == null) {
				return false;
			}

			return await this.AuthenticateUserForSensor(sensor, strict).ConfigureAwait(false);
		}

		protected async Task<bool> AuthenticateUserForSensor(Sensor sensor, bool strict)
		{
			if(sensor == null) {
				throw new ArgumentNullException(nameof(sensor));
			}

			var sensorKey = await this.m_keys.GetAsync(sensor.Secret).ConfigureAwait(false);
			var auth = sensor.Owner == this.m_currentUserId && sensorKey != null;

			if(this.ApiKey == null) {
				return false;
			}

			auth = auth && !this.ApiKey.Revoked;

			if(strict) {
				auth = auth && this.ApiKey.Type == ApiKeyType.SensorKey;
				auth = auth && this.ApiKey.Key == sensor.Secret;
			}

			var isHealthyUser = this.CurrentUser.UserRoles.Any(role => role != UserRoles.Banned.ToUpperInvariant());

			return auth && isHealthyUser;
		}

		protected StatusCodeResult Forbidden()
		{
			return this.StatusCode(403);
		}
	}
}

