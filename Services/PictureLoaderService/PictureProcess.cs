using System;
using System.Collections.Generic;
using System.Threading;
using Services.PreloadAssetService;
using UnityEngine;
using UnityEngine.UI;

namespace Services.PictureLoaderService
{
    /// <summary>
    /// PictureProcess - A Run-Time image processing.
    /// Fluid interface methods can be called in any order,
    /// duplicating calls methods results in multiple executing.
    /// To start processing, need add call Process() at Fluid interface end.
    /// </summary>
    public class PictureProcess : IDisposable
    {
        private readonly CancellationTokenSource tokenSource;
        private readonly IPreloaderService<Texture2D> preloaderService;
        private readonly List<IAnimation> animations;
        private readonly string targetUrl;
        private Action<Texture2D> intoAction;
        private Action placeHolderAction;
        private Texture2D processTexture;
        private Action<Texture2D> completeAction;
        
        public PictureProcess(IPreloaderService<Texture2D> preloaderService, string targetUrl)
        {
            if (string.IsNullOrEmpty(targetUrl))
            {
                throw new InvalidOperationException("Url is empty");
            }

            if (preloaderService == null)
            {
                throw new InvalidOperationException("Preloader service does not exist");
            }
            
            tokenSource = new CancellationTokenSource();
            animations = new List<IAnimation>();
            this.preloaderService = preloaderService;
            this.targetUrl = targetUrl;
        }

        public PictureProcess Into(Image placeHolder, float? refWidth = null, float? refHeight = null)
        {
            var width = refWidth ?? processTexture.width;
            var height = refHeight ?? processTexture.height;
            var rect = new Rect(0, 0, width, height);
            var pivot = new Vector2(0.5f, 0.5f);
            var action = new Action<Texture2D>(result =>
            {
                if (!placeHolder) return;
                placeHolder.sprite = Sprite.Create(result, rect, pivot);
            });
            
            intoAction += action;
            placeHolderAction += action;
            return this;
        }

        public PictureProcess Into(Renderer placeHolder)
        {
            var action = new Action<Texture2D>(result =>
            {
                if (!placeHolder) return;
                placeHolder.material.mainTexture = result;
            });
            intoAction += action;
            placeHolderAction += action;
            return this;
        }

        public PictureProcess Into(RawImage placeHolder)
        {
            var action = new Action<Texture2D>(result =>
            {
                if (!placeHolder) return;
                placeHolder.material.mainTexture = result;
            });
            intoAction += action;
            placeHolderAction += action;
            return this;
        }
        
        public PictureProcess ProcessPlaceHolder(Texture2D texture)
        {
            processTexture = texture;
            return this;
        }

        public PictureProcess AddAnimation(IAnimation animation)
        {
            if (animation == null)
                return this;
            animations.Add(animation);
            return this;
        }

        public PictureProcess OnComplete(Action onComplete)
        {
            completeAction = onComplete;
            return this;
        }
        
        public IDisposable Process(bool cached = true)
        {
            if (processTexture)
            {
                placeHolderAction?.Invoke(processTexture);
            }
            animations.ForEach(x=>x.Play());

            var process = preloaderService.Get(targetUrl, cached, tokenSource.Token);
            process.GetAwaiter().OnCompleted(() =>
            {
                if (process.Result)
                    intoAction?.Invoke(process.Result);
                
                completeAction?.Invoke();
                Dispose();
            });
            return this;
        }

        /// <summary>
        /// Abort any process, do not use class after Dispose
        /// </summary>
        public void Dispose()
        {
            if (tokenSource.IsCancellationRequested)
            {
                return;
            }
            animations.ForEach(x=>x.Dispose());
            animations.Clear();
            intoAction = null;
            placeHolderAction = null;
            completeAction = null;
            processTexture = null;
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
    }
}