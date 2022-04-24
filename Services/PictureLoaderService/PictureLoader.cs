using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Services.CacheService;
using Services.HashGeneratorService;
using Services.PreloadAssetService;
using UnityEngine;

namespace Services.PictureLoaderService
{
    public static class PictureLoader
    {
        private static IPreloaderService<Texture2D> preloaderService;
        private static Dictionary<string, PictureProcess> processes;
        private static bool initialized;
        /// <summary>
        /// Befor use, need setup.
        /// </summary>
        /// <param name="pictureUrls">To preload images use array urls</param>
        /// <returns></returns>
        public static async Task Setup(IEnumerable<string> pictureUrls = null)
        {
            if (initialized)
            {
                return;
            }

            pictureUrls = pictureUrls ?? new string[0];
            var cachePath = Application.persistentDataPath + "/" + "PictureCache" + "/";
            var cacheService = new LocalCacheService(cachePath, new MD5HashGenerator());
            preloaderService = new TexturePreloaderService(pictureUrls, cacheService, cacheService);
            processes = new Dictionary<string, PictureProcess>();
            initialized = true;
            await preloaderService.Preload();
        }
        
        /// <summary>
        /// Create process or get layer-process
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cached"></param>
        /// <returns></returns>
        public static PictureProcess GetProcess(string url, bool cached = true)
        {
	        if (!initialized)
	        {
		        Debug.LogError("Picture Loader not initialized");
		        return default;
	        }
	        
	        if (processes.ContainsKey(url))
	        {
		        var currProcess = processes[url];
		        if (!currProcess.Disposed)
			        return currProcess.GetLayerProcess(forcibly: true);

			if(currProcess.Disposed)
				processes.Remove(url);
	        }
	        
	        var process = new PictureProcess(url, cached, preloaderService);
	        processes.Add(url, process.OnDispose(() => processes.Remove(url)));
	        return process;
        }
    }
}
