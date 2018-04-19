﻿/*
 * Sensor statistics implementation.
 *
 * @author Michel Megens
 * @email  dev@bietje.net
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;
using SensateService.Helpers;
using SensateService.Infrastructure.Repositories;
using SensateService.Models;

namespace SensateService.Infrastructure.Document
{
	public class SensorStatisticsRepositoryRepository : AbstractDocumentRepository<string, SensorStatisticsEntry>, ISensorStatisticsRepository
	{
		private readonly ILogger<SensorStatisticsRepositoryRepository> _logger;
		private readonly IMongoCollection<SensorStatisticsEntry> _stats;

		public SensorStatisticsRepositoryRepository(SensateContext context, ILogger<SensorStatisticsRepositoryRepository> logger) : base(context)
		{
			this._logger = logger;
			this._stats = context.Statistics;
		}

		public override void Delete(string id)
		{
			ObjectId objectId;

			objectId = ObjectId.Parse(id);
			this._stats.DeleteOne(x => x.InternalId == objectId);
		}

		public override async Task DeleteAsync(string id)
		{
			ObjectId objectId;

			objectId = ObjectId.Parse(id);
			await this._stats.DeleteOneAsync(x => x.InternalId == objectId);
		}

		public async Task DeleteBySensorAsync(Sensor sensor)
		{
			var query = Builders<SensorStatisticsEntry>.Filter.Eq("SensorId", sensor.InternalId);

			try {
				await this._stats.DeleteManyAsync(query);
			} catch(Exception ex) {
				this._logger.LogWarning(ex.Message);
			}
		}

#region Entry creation

		public async Task IncrementAsync(Sensor sensor)
		{
			SensorStatisticsEntry entry;
			var update = Builders<SensorStatisticsEntry>.Update;
			UpdateDefinition<SensorStatisticsEntry> updateDefinition;

			entry = await this.GetByDateAsync(sensor, DateTime.Today) ?? await this.CreateForAsync(sensor);
			updateDefinition = update.Inc(x => x.Measurements, 1);
			await this._stats.FindOneAndUpdateAsync(x => x.InternalId == entry.InternalId, updateDefinition);
		}

		public async Task<SensorStatisticsEntry> CreateForAsync(Sensor sensor)
		{
			SensorStatisticsEntry entry;

			entry = new SensorStatisticsEntry {
				InternalId = base.GenerateId(DateTime.Now),
				Date = DateTime.Today,
				Measurements = 0,
				SensorId = sensor.InternalId
			};

			await this.CreateAsync(entry);
			return entry;
		}

		public override void Create(SensorStatisticsEntry obj)
		{
			try {
				this._stats.InsertOne(obj);
			} catch(Exception ex) {
				this._logger.LogWarning(ex.Message);
				throw ex;
			}
		}

		public override async Task CreateAsync(SensorStatisticsEntry obj)
		{
			try {
				await this._stats.InsertOneAsync(obj);
			} catch(Exception ex) {
				this._logger.LogWarning(ex.Message);
				throw ex;
			}
		}

#endregion

#region Entry Getters

		public async Task<SensorStatisticsEntry> GetByDateAsync(Sensor sensor, DateTime date)
		{
			FilterDefinition<SensorStatisticsEntry> filter;
			var filterBuilder = Builders<SensorStatisticsEntry>.Filter;

			filter = filterBuilder.Eq("SensorId", sensor.InternalId) & filterBuilder.Eq("Date", date);
			var result = await this._stats.FindAsync(filter);

			if(result == null)
				return null;

			return await result.FirstOrDefaultAsync().AwaitSafely();
		}

		public async Task<IEnumerable<SensorStatisticsEntry>> GetBeforeAsync(Sensor sensor, DateTime date)
		{
			FilterDefinition<SensorStatisticsEntry> filter;
			var filterBuilder = Builders<SensorStatisticsEntry>.Filter;

			filter = filterBuilder.Eq("SensorId", sensor.InternalId) & filterBuilder.Lte("Date", date);
			var result = await this._stats.FindAsync(filter);

			if(result == null)
				return null;

			return await result.ToListAsync().AwaitSafely();
		}

		public async Task<IEnumerable<SensorStatisticsEntry>> GetAfterAsync(Sensor sensor, DateTime date)
		{
			FilterDefinition<SensorStatisticsEntry> filter;
			var filterBuilder = Builders<SensorStatisticsEntry>.Filter;

			filter = filterBuilder.Eq("SensorId", sensor.InternalId) & filterBuilder.Gte("Date", date);
			var result = await this._stats.FindAsync(filter);

			if(result == null)
				return null;

			return await result.ToListAsync().AwaitSafely();
		}

#endregion

#region Not implemented

		public override void Update(SensorStatisticsEntry obj)
		{
			throw new InvalidOperationException("SensorStatisticsRepository.Update is an invalid operation!");
		}

		public override void Commit(SensorStatisticsEntry obj)
		{
			throw new NotImplementedException();
		}

		public override Task CommitAsync(SensorStatisticsEntry obj, CancellationToken cto = default(CancellationToken))
		{
			throw new NotImplementedException();
		}


		public override SensorStatisticsEntry GetById(string id)
		{
			throw new InvalidOperationException("SensorStatisticsRepository.GetById is an invalid operation!");
		}

#endregion
	}
}