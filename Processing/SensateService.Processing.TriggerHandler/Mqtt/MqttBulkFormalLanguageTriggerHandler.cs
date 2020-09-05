﻿/*
 * MQTT formal language trigger hanlder.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.GeoJsonObjectModel;
using SensateService.Common.Data.Dto.Protobuf;
using SensateService.Common.Data.Models;
using SensateService.Helpers;
using SensateService.Infrastructure.Repositories;
using SensateService.Middleware;
using SensateService.Processing.TriggerHandler.Models;
using SensateService.Processing.TriggerHandler.Services;
using Message = SensateService.Processing.TriggerHandler.Models.Message;

namespace SensateService.Processing.TriggerHandler.Mqtt
{
	public class MqttBulkFormalLanguageTriggerHandler : MqttHandler
	{
		private readonly ILogger<MqttBulkFormalLanguageTriggerHandler> m_logger;
		private readonly ITriggerRepository m_triggers;
		private readonly ITriggerTextMatchingService m_matcher;

		public MqttBulkFormalLanguageTriggerHandler(
			ILogger<MqttBulkFormalLanguageTriggerHandler> logger,
			ITriggerRepository triggers,
			ITriggerTextMatchingService matcher
		)
		{
			this.m_logger = logger;
			this.m_triggers = triggers;
			this.m_matcher = matcher;
		}

		private static IEnumerable<InternalBulkMessageQueue> Decompress(string data)
		{
			var bytes = Convert.FromBase64String(data);
			using var to = new MemoryStream();
			using var from = new MemoryStream(bytes);
			using var gzip = new GZipStream(@from, CompressionMode.Decompress);

			gzip.CopyTo(to);
			var final = to.ToArray();
			var protoMeasurements = TextMessageData.Parser.ParseFrom(final);
			var messages =
				from message in protoMeasurements.Messages
				group message by message.SensorId into g
				select new InternalBulkMessageQueue {
					SensorId = g.Key,
					Messages = g.Select(m => new Message {
						Data = m.Data,
						Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(m.Longitude, m.Latitude)),
						Timestamp = DateTime.Parse(m.Timestamp)
					}).ToList()
				};

			return messages;
		}

		public override void OnMessage(string topic, string msg)
		{
			Task.Run(async () => { await this.OnMessageAsync(topic, msg).AwaitBackground(); }).Wait();
		}

		public override async Task OnMessageAsync(string topic, string message)
		{
			this.m_logger.LogDebug("Message received!");

			try {
				var messages = Decompress(message).ToList();

				var ids = messages.Select(m => m.SensorId).Distinct().ToList();

				var raw_triggers = await this.m_triggers.GetAsync(ids, TriggerType.Regex).AwaitBackground();
				var triggers = raw_triggers.ToList();
				var triggered = new List<Tuple<Trigger, TriggerInvocation>>();
				var regexes = triggers.Select(x => new Tuple<Trigger, Regex>(
												  x, new Regex(x.FormalLanguage)
												  )).ToList();

				foreach(var mCollection in messages) {
					var subset = regexes.Where(trigger => trigger.Item1.SensorId == mCollection.SensorId).ToList();

					if(subset.Count <= 0) {
						continue;
					}

					foreach(var msg in mCollection.Messages) {
						triggered.AddRange(
							from t in subset
							where t.Item2.IsMatch(msg.Data)
							let inv = new TriggerInvocation {
								TriggerId = t.Item1.Id,
								Timestamp = new DateTimeOffset(msg.Timestamp.ToUniversalTime(), TimeSpan.Zero)
							}
							select new Tuple<Trigger, TriggerInvocation>(t.Item1, inv));
					}
				}

				var distinct = triggered.GroupBy(t => t.Item2.TriggerId)
					.Select(g => g.First()).ToList();

				await this.m_matcher.HandleTriggerAsync(distinct).AwaitBackground();
				await this.m_triggers.AddInvocationsAsync(distinct.Select(t => t.Item2)).AwaitBackground();

			} catch(Exception ex) {
				this.m_logger.LogInformation($"Unable to handle message trigger: {ex.Message}");
				this.m_logger.LogDebug(ex.StackTrace);
			}
		}
	}
}