using System.Collections.Generic;
using System.Threading.Tasks;
using Services.CacheService;
using Services.HashGeneratorService;
using Services.PreloadAssetService;
using UnityEngine;

namespace Services.PictureLoaderService
{
    public static class PictureLoader
    {
        private static IPreloaderService<Texture2D> _preloaderService;

        public static async Task Setup(IEnumerable<string> pictureUrls = null)
        {
            if (_preloaderService != null)
            {
                return;
            }

            pictureUrls = pictureUrls ?? new string[0];
            var cachePath = Application.persistentDataPath + "/" + "PictureCache" + "/";
            var cacheService = new LocalCacheService(cachePath, new MD5HashGenerator());
            _preloaderService = new TexturePreloaderService(pictureUrls, cacheService, cacheService);
            await _preloaderService.Preload();
        }

        public static PictureProcess Init(string url)
        {
            return new PictureProcess(_preloaderService, url);
        }
    }
}