using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Services.CacheService;
using Services.DownloaderService;
using UnityEngine;
using UnityEngine.Networking;

namespace Services.PreloadAssetService
{
    public class TexturePreloaderService : IPreloaderService<Texture2D>
    {
        private readonly ICacheWriter cacheWriter;
        private readonly ICacheProvider cacheProvider;
        private readonly IDownloader<DownloadHandler> downloader;
        private readonly List<string> assetLinks;
        private readonly int maxParallelProcess;
        private int parallelProcessCount;

        public TexturePreloaderService(IEnumerable<string> urls,
                                       ICacheWriter cacheWriter,
                                       ICacheProvider cacheProvider,
                                       int maxParallelProcess = -1)
        {
            assetLinks = urls?.ToList() ?? new List<string>();
            downloader = new DefaultDownloader();
            this.cacheWriter = cacheWriter;
            this.cacheProvider = cacheProvider;
            maxParallelProcess = maxParallelProcess == 0 ? -1 : maxParallelProcess;
            this.maxParallelProcess = Mathf.Max(-1, maxParallelProcess);
        }

        public async Task Preload(CancellationToken token = default)
        {
            try
            {
                foreach (var url in assetLinks)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (maxParallelProcess == -1 || parallelProcessCount < maxParallelProcess)
                    {
                        parallelProcessCount++;
                        var hasEntry = cacheProvider.Contains(url, token);
                        hasEntry.GetAwaiter().OnCompleted(() =>
                        {
                            if (hasEntry.Result)
                            {
                                parallelProcessCount--;
                                return;
                            }

                            var download = downloader.Download(url, token);
                            download.GetAwaiter().OnCompleted(() =>
                            {
                                var data = download.Result.data;
                                if (data == null || data.Length == 0)
                                {
                                    parallelProcessCount--;
                                    return;
                                }

                                var write = cacheWriter.Write(data, url, token);
                                write.GetAwaiter().OnCompleted(() => parallelProcessCount--);
                            });
                        });
                    }

                    while (maxParallelProcess > 0
                           && parallelProcessCount >= maxParallelProcess
                           && Application.isPlaying)
                    {
                        await Task.Yield();
                    }
                }

                while (parallelProcessCount > 0
                       && Application.isPlaying)
                {
                    await Task.Yield();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public async Task Unload(CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            
            var unloadProcess = assetLinks.Count;
            foreach (var url in assetLinks.ToArray())
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                
                cacheWriter
                    .Remove(url, token)
                    .GetAwaiter()
                    .OnCompleted(() => unloadProcess--);
            }

            while (unloadProcess > 0 && !token.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }

        public async Task<Texture2D> Get(string url, bool preload = true, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return default;
            }

            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("Input url string is empty");
                return default;
            }
            
            byte[] data;
            if (preload)
            {
                if (await cacheProvider.Contains(url, token))
                {
                    data = await cacheProvider.Get(url, token);
                }
                else
                {
                    assetLinks.Add(url);
                    var download = await downloader.Download(url, token);
                    if (token.IsCancellationRequested
                        || download.data == null 
                        || download.data.Length == 0)
                    {
                        return null;
                    }
                    data = download.data;
                    await cacheWriter.Write(data, url, token);
                }
            }
            else
            {
                var download = await downloader.Download(url, token);
                if (token.IsCancellationRequested
                    || download.data == null 
                    || download.data.Length == 0)
                {
                    return null;
                }
                data = download.data;
            }

            if (token.IsCancellationRequested)
            {
                return default;
            }
            
            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(data))
            {
                Debug.LogError($"Texture does not created from raw data : {url}");
            }
            return texture;
        }
    }
}