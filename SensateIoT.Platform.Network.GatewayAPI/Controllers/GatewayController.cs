/*
 * Message gateway controller.
 *
 * @author Michel Megens
 * @email michel@michelmegens.net
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using Newtonsoft.Json;

using SensateIoT.Platform.Network.Common.Adapters.Abstract;
using SensateIoT.Platform.Network.Common.Converters;
using SensateIoT.Platform.Network.Common.Services.Processing;
using SensateIoT.Platform.Network.Data.Abstract;
using SensateIoT.Platform.Network.Data.Models;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using SensateIoT.Platform.Network.GatewayAPI.Abstract;
using SensateIoT.Platform.Network.GatewayAPI.Attributes;
using SensateIoT.Platform.Network.GatewayAPI.DTO;

using Measurement = SensateIoT.Platform.Network.GatewayAPI.DTO.Measurement;
using Message = SensateIoT.Platform.Network.GatewayAPI.DTO.Message;

namespace SensateIoT.Platform.Network.GatewayAPI.Controllers
{
	[Produces("application/json")]
	[Route("gateway/v{version:apiVersion}")]
	[ApiVersion("2"), ApiVersion("1")]
	[ApiController]
	public class GatewayController : AbstractApiController
	{
		private readonly IMeasurementAuthorizationService m_measurementAuthorizationService;
		private readonly IMessageAuthorizationService m_messageAuthorizationService;
		private readonly IBulkMessageAuthorizationService m_bulkMessageAuthorizationService;
		private readonly IBulkMeasurementAuthorizationService m_bulkMeasurementAuthorizationService;
		private readonly IBlobRepository m_blobs;
		private readonly IBlobService m_blobService;
		private readonly IRouterClient m_client;
		private readonly ILogger<GatewayController> m_logger;
		private readonly IAuthorizationService m_auth;

		public GatewayController(IMeasurementAuthorizationService measurementAuth,
								 IMessageAuthorizationService messageAuth,
								 IBulkMessageAuthorizationService bulkMessageAuth,
								 IBulkMeasurementAuthorizationService bulkMeasurementAuth,
								 IHttpContextAccessor ctx,
								 ISensorRepository sensors,
								 IApiKeyRepository keys,
								 IBlobRepository blobs,
								 IBlobService blobService,
								 IRouterClient client,
								 IAuthorizationService auth,
								 ILogger<GatewayController> logger) : base(ctx, sensors, keys)
		{
			this.m_measurementAuthorizationService = measurementAuth;
			this.m_messageAuthorizationService = messageAuth;
			this.m_bulkMessageAuthorizationService = bulkMessageAuth;
			this.m_bulkMeasurementAuthorizationService = bulkMeasurementAuth;
			this.m_blobService = blobService;
			this.m_blobs = blobs;
			this.m_client = client;
			this.m_logger = logger;
			this.m_auth = auth;
		}

		[MapToApiVersion("1")]
		[HttpPost("blobs")]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status422UnprocessableEntity)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Response<GatewayResponse>), StatusCodes.Status202Accepted)]
		public async Task<IActionResult> CreateBlobAsync([FromForm] FileUploadForm upload)
		{
			var file = upload.File;

			if(file == null) {
				var response = new Response<object>();

				foreach(var error in this.ModelState.Values.SelectMany(modelState => modelState.Errors)) {
					response.AddError(error.ErrorMessage);
				}

				return this.UnprocessableEntity(response);
			}

			this.m_logger.LogDebug($"Creating file/blob: {file.Name}");

			var blob = new Blob {
				SensorID = upload.SensorId,
				FileName = upload.Name,
				Timestamp = DateTime.UtcNow,
				FileSize = Convert.ToInt32(upload.File.Length)
			};

			var auth = await this.AuthenticateUserForSensor(upload.SensorId, true).ConfigureAwait(false);

			if(!auth) {
				return this.Forbidden();
			}

			await using var mstream = new MemoryStream();
			await upload.File.CopyToAsync(mstream);
			await this.m_blobService.StoreAsync(blob, mstream.ToArray()).ConfigureAwait(false);
			await this.m_blobs.CreateAsync(blob).ConfigureAwait(false);

			var resp = new Response<GatewayResponse> { Data = { Message = "Blob created!", Queued = 1 } };

			return this.Accepted(resp);
		}

		[MapToApiVersion("2")]
		[HttpPost("messages")]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Response<GatewayResponse>), StatusCodes.Status202Accepted)]
		public async Task<IActionResult> EnqueueBulkMessagesAsync([FromBody] GenericGatewayRequest<MessagePart> messages)
		{
			await Task.CompletedTask;
			var response = new Response<GatewayResponse>();

			var raw = await this.PeekRequestBodyAsString().ConfigureAwait(false);
			this.m_bulkMessageAuthorizationService.AddMessage(new JsonGatewayRequest<MessagePart> {
				RequestData = messages,
				Json = raw
			});

			response.Data = new GatewayResponse {
				Message = "Messages received and queued.",
				Queued = messages.Values.Count()
			};

			return this.Accepted(response);
		}

		[MapToApiVersion("1")]
		[HttpPost("messages")]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Response<GatewayResponse>), StatusCodes.Status202Accepted)]
		public async Task<IActionResult> EnqueueMessageAsync([FromBody] Message message)
		{
			var response = new Response<GatewayResponse>();

			var raw = await this.PeekRequestBodyAsString().ConfigureAwait(false);
			this.m_messageAuthorizationService.AddMessage(new JsonMessage(message, raw));

			response.Data = new GatewayResponse {
				Message = "Messages received and queued.",
				Queued = 1
			};

			return this.Accepted(response);
		}

		[MapToApiVersion("2")]
		[HttpPost("measurements")]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Response<GatewayResponse>), StatusCodes.Status202Accepted)]
		public async Task<IActionResult> EnqueueBulkMeasurements([FromBody] GenericGatewayRequest<MeasurementPart> measurements)
		{
			var response = new Response<GatewayResponse>();

			var raw = await this.PeekRequestBodyAsString().ConfigureAwait(false);
			this.m_bulkMeasurementAuthorizationService.AddMessage(new JsonGatewayRequest<MeasurementPart> {
				RequestData = measurements,
				Json = raw
			});

			response.Data = new GatewayResponse {
				Message = "Measurements received and queued.",
				Queued = measurements.Values.Count()
			};

			return this.Accepted(response);
		}

		[HttpPost("measurements")]
		[ReadWriteApiKey, ValidateModel]
		[MapToApiVersion("1")]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Response<GatewayResponse>), StatusCodes.Status202Accepted)]
		public async Task<IActionResult> EnqueueMeasurement([FromBody] Measurement measurement)
		{
			var response = new Response<GatewayResponse>();

			var raw = await this.PeekRequestBodyAsString().ConfigureAwait(false);
			this.m_measurementAuthorizationService.AddMessage(new JsonMeasurement(measurement, raw));

			response.Data = new GatewayResponse {
				Message = "Measurements received and queued.",
				Queued = 1
			};

			return this.Accepted(response);
		}

		[MapToApiVersion("1")]
		[HttpPost("actuators")]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Response<GatewayResponse>), StatusCodes.Status202Accepted)]
		public async Task<IActionResult> EnqueueControlMessageAsync([FromBody] ActuatorMessage message)
		{
			var response = new Response<GatewayResponse>();

			if(!ObjectId.TryParse(message.SensorId, out var id)) {
				response.AddError("Invalid sensor ID.");
				return this.BadRequest(response);
			}

			var sensor = await this.m_sensors.GetAsync(id).ConfigureAwait(false);
			var auth = await this.AuthenticateUserForSensor(sensor, false).ConfigureAwait(false);

			if(!auth) {
				response.AddError($"Sensor {message.SensorId} is not authorized for user {this.m_currentUserId}");
				return this.Unauthorized(response);
			}

			var controlMessage = new Data.DTO.ControlMessage {
				Data = message.Data,
				Destination = ControlMessageType.Mqtt,
				PlatformTimestamp = DateTime.UtcNow,
				Timestamp = DateTime.UtcNow,
				Secret = sensor.Secret,
				SensorId = id
			};

			var json = JsonConvert.SerializeObject(controlMessage);
			this.m_auth.SignControlMessage(controlMessage, json);

			await this.m_client.RouteAsync(ControlMessageProtobufConverter.Convert(controlMessage), CancellationToken.None)
				.ConfigureAwait(false);

			response.Data = new GatewayResponse {
				Message = "Control message received and queued.",
				Queued = 1
			};

			return this.Ok(response);
		}

		private async Task<string> PeekRequestBodyAsString()
		{
			try {
				var buffer = new byte[Convert.ToInt32(this.Request.ContentLength)];
				this.Request.Body.Position = 0;

				await this.Request.Body.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).ConfigureAwait(false);
				return Encoding.UTF8.GetString(buffer);
			} finally {
				this.Request.Body.Position = 0;
			}
		}
	}
}
