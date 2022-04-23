using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Services.HashGeneratorService;
using UnityEngine;

namespace Services.CacheService
{
	public class LocalCacheService : ICacheProvider, ICacheWriter
	{
		private readonly string path;
		private readonly IHashGenerator hashGenerator;

		public LocalCacheService(string path, IHashGenerator hashGenerator)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Path argument is empty");
			}

			this.path = path;
			this.hashGenerator = hashGenerator ?? throw new ArgumentException("Hash generator does not exits");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

		public Task<bool> Contains(string id, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
			{
				return Task.FromCanceled<bool>(token);
			}
			
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentException("Input id is empty");
			}
			
			return Task.FromResult(File.Exists(path + hashGenerator.GetHash(id)));
		}

		public Task<byte[]> Get(string id, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
			{
				return Task.FromCanceled<byte[]>(token);
			}
			
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentException("Input id is empty");
			}
			
			return Task.FromResult(File.ReadAllBytes(path + hashGenerator.GetHash(id)));
		}

		public Task Write(byte[] value, string id, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
			{
				return Task.FromCanceled(token);
			}
			
			try
			{
				File.WriteAllBytes(path + hashGenerator.GetHash(id), value);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}

			return Task.CompletedTask;
		}

		public Task Remove(string id, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
			{
				return Task.FromCanceled(token);
			}
			
			try
			{
				File.Delete(path + hashGenerator.GetHash(id));
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}

			return Task.CompletedTask;
		}

		public Task Clear(CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
			{
				return Task.FromCanceled(token);
			}
			
			try
			{
				var directoryInfo = new DirectoryInfo(path);
				File.Delete(path);
				foreach (var file in directoryInfo.GetFiles())
				{
					if (token.IsCancellationRequested)
					{
						return Task.FromCanceled(token);
					}
					file.Delete();
				}
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}

			return Task.CompletedTask;
		}
	}
}