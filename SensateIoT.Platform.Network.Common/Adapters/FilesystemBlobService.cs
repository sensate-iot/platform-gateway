﻿/*
 * Filesystem blob service implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SensateIoT.Platform.Network.Common.Adapters.Abstract;
using SensateIoT.Platform.Network.Common.Settings;
using SensateIoT.Platform.Network.Data.Models;

namespace SensateIoT.Platform.Network.Common.Adapters
{
	public class FilesystemBlobService : IBlobService
	{
		private readonly string m_path;

		private string BasePath => $"{this.m_path}{Path.DirectorySeparatorChar}";

		public FilesystemBlobService(IOptions<BlobOptions> options)
		{
			this.m_path = options.Value.StoragePath;
		}

		public async Task StoreAsync(Blob blob, byte[] data, CancellationToken ct = default)
		{
			blob.Path = $"{this.BasePath}{blob.SensorID}";
			blob.StorageType = StorageType.FileSystem;

			Directory.CreateDirectory(blob.Path);
			using(var stream = new FileStream($"{blob.Path}{Path.DirectorySeparatorChar}{blob.FileName}", FileMode.Create)) {
				await stream.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);
			}
		}
	}
}
