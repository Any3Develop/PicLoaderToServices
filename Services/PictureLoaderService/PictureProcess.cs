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
	/// Duplicating calls methods results in multiple executing.
	/// <code>
	/// 
	/// PictureLoader
	/// 	.GetProcess(url)
	/// 	.GetLayerProcess(forcibly) (call first, used automatically if head process by url alredy exist)
	/// 	.Into(Image)
	/// 	.Into(RawImage)
	///	.Into(Renderer)
	/// 	.Into(Renderer.Material) (no call restrictions, easy add another type place holders)
	/// 	.SetWaitTexture(LoadingTexture) (no call restrictions, after Into)
	/// 	.SetAnimation(Animation)
	/// 	.SetAnimation(AnotherAnimation1) (no call restrictions)
	/// 	.OnComplete(Action)
	/// 	.OnComplete(Action1) 
	/// 	.OnDispose(Action2)
	/// 	.OnDispose(Action3) (callbacks no call restrictions, anywhere position, automatically disposed)
	/// 	.Run() (at end to start process)
	/// </code>
	/// </summary>
	public class PictureProcess
	{
		public bool Disposed { get; private set; }
		public bool IsLayerProcess => headProcess != this;
		
		private readonly CancellationTokenSource tokenSource;
		private readonly IPreloaderService<Texture2D> preloaderService;
		private readonly List<IAnimation> animations;
		private readonly PictureProcess headProcess;
		private readonly bool preload;
		private readonly string url;
		
		private Action<Texture2D> waitAction;
		private Action<Texture2D> intoAction;
		private Texture2D waitTexture;
		private Action completeAction;
		private Action disposeAction;
		private bool completed;
		private bool running;

		/// <summary>
		/// The process can be multi-level.
		/// Each process is responsible only for its data set to it by the user.
		/// The process waits for an image and plays user actions.
		/// </summary>
		/// <param name="url">Target image url to dowload</param>
		/// <param name="preload">Cache the image by CacheServise</param>
		/// <param name="preloaderService">Image preloader, read image from cache or cache downloaded images.</param>
		/// <param name="headProcess">Set head process if need multy-level process</param>
		public PictureProcess(string url,
		                      bool preload,
		                      IPreloaderService<Texture2D> preloaderService,
		                      PictureProcess headProcess = null)
		{
			if (string.IsNullOrEmpty(url))
			{
				Debug.LogError("Url is empty");
				return;
			}

			if (preloaderService == null)
			{
				Debug.LogError("Preloader service does not exist");
				return;
			}

			this.url = url;
			this.preload = preload;
			tokenSource = new CancellationTokenSource();
			animations = new List<IAnimation>();
			this.headProcess = headProcess ?? this;
			this.preloaderService = preloaderService;
		}

		/// <summary>
		/// Place result of process in PlaceHolders
		/// </summary>
		/// <param name="placeHolder">Target into result placed</param>
		/// <param name="makeCopy">Make copy of result or use instance by default</param>
		/// <param name="refWidth">If default the width of the result is used</param>
		/// <param name="refHeight">If default the height of the result is used</param>
		/// <returns></returns>
		public PictureProcess Into(Image placeHolder,
		                           bool makeCopy = false,
		                           float? refWidth = null,
		                           float? refHeight = null)
		{
			void SetInto(Texture2D result)
			{
				if (!placeHolder || !result) return;
				var width = refWidth ?? result.width;
				var height = refHeight ?? result.height;
				var rect = new Rect(0, 0, width, height);
				var pivot = new Vector2(0.5f, 0.5f);
				placeHolder.sprite = Sprite.Create(GetCopy(result, makeCopy), rect, pivot);
			}

			intoAction += SetInto;
			waitAction += SetInto;
			return this;
		}

		/// <summary>
		/// Place result of process in PlaceHolders
		/// </summary>
		/// <param name="placeHolder">Target into result placed</param>
		/// <param name="makeCopy">Make copy of result or use instance by default</param>
		/// <returns></returns>
		public PictureProcess Into(RawImage placeHolder, bool makeCopy = false)
		{
			void SetInto(Texture2D result)
			{
				if (!placeHolder || !result) return;
				placeHolder.texture = GetCopy(result, makeCopy);
			}

			intoAction += SetInto;
			waitAction += SetInto;
			return this;
		}
		
		/// <summary>
		/// Place result of process in PlaceHolders
		/// </summary>
		/// <param name="placeHolder">Target into result placed</param>
		/// <param name="makeCopy">Make copy of result or use instance by default</param>
		/// <returns></returns>
		public PictureProcess Into(Material placeHolder, bool makeCopy = false)
		{
			void SetInto(Texture2D result)
			{
				if (!placeHolder || !result) return;
				placeHolder.mainTexture = GetCopy(result, makeCopy);
			}

			intoAction += SetInto;
			waitAction += SetInto;
			return this;
		}
		
		/// <summary>
		/// Place result of process in PlaceHolders
		/// </summary>
		/// <param name="placeHolder">Target into result placed</param>
		/// <param name="makeCopy">Make copy of result or use instance by default</param>
		/// <returns></returns>
		public PictureProcess Into(Renderer placeHolder, bool makeCopy = false)
		{
			return placeHolder 
				? Into(placeHolder.material, makeCopy)
				: this;
		}

		/// <summary>
		/// Set texure into PlaceHolders while the process is going on.
		/// </summary>
		/// <returns></returns>
		public PictureProcess SetWaitTexture(Texture2D texture)
		{
			if (!texture || headProcess.Disposed)
				return this;

			waitTexture = texture;
			return this;
		}

		/// <summary>
		/// Custom animations will played while process is going on.
		/// </summary>
		/// <param name="animation">Custom animation</param>
		/// <returns></returns>
		public PictureProcess SetAnimation(IAnimation animation)
		{
			if (animation == null || headProcess.Disposed)
				return this;

			animations.Add(animation);
			return this;
		}

		/// <summary>
		/// Creates a process layer above the head process.
		/// Allows you to add additional actions while a single process is busy loading data.
		/// You can kill a layer process independent of the main process.
		/// </summary>
		/// <param name="forcibly">
		/// Allows you to create a layer forcibly.
		/// Any layers cannot reach a depth of 1 layer from head process.
		/// Otherwise a new layer will be created from the head process.</param>
		/// <returns></returns>
		public PictureProcess GetLayerProcess(bool forcibly = false)
		{
			if (!forcibly && IsLayerProcess) return this;

			var layerProcess = new PictureProcess(url, preload, preloaderService, headProcess);
			headProcess.completeAction += layerProcess.Run;
			headProcess.disposeAction += layerProcess.Run;
			return layerProcess;
		}

		/// <summary>
		/// Set callback on complete of head or layer processes
		/// </summary>
		/// <param name="action">Target callback</param>
		/// <returns></returns>
		public PictureProcess OnComplete(Action action)
		{
			completeAction += action;
			return this;
		}

		/// <summary>
		/// Set callback on dispose of head or layer processes
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public PictureProcess OnDispose(Action action)
		{
			disposeAction += action;
			return this;
		}

		/// <summary>
		/// Running process of Fluid interface.
		/// </summary>
		public void Run()
		{
			if (IsLayerProcess)
			{
				if (headProcess.running && !running)
				{
					HandlePreProcess();
					if (!headProcess.completed) return;
				}
				
				HandleIntoProcess();
				return;
			}

			if (running || Disposed) return;
			
			HandlePreProcess();
			HandleIntoProcess();
		}
		
		/// <summary>
		/// Dispose head process and all layer-processes.
		/// </summary>
		public void Dispose()
		{
			if (headProcess.Disposed)
				return;

			headProcess.Disposed = true;
			headProcess.running = false;
			headProcess.tokenSource.Cancel();
			headProcess.animations.ForEach(x => x?.Dispose());
			headProcess.animations.Clear();
			headProcess.disposeAction?.Invoke();
			headProcess.completeAction = null;
			headProcess.disposeAction = null;
			headProcess.waitTexture = null;
			headProcess.intoAction = null;
			headProcess.waitAction = null;
		}

		/// <summary>
		/// Dispose only current layer-process
		/// </summary>
		public void DisposeLayer()
		{
			if (!IsLayerProcess || !running)
				return;

			if (!tokenSource.IsCancellationRequested)
				tokenSource.Cancel();

			Disposed = true;
			animations.ForEach(x => x?.Dispose());
			animations.Clear();
			disposeAction?.Invoke();
			disposeAction = null;
			intoAction = null;
			waitAction = null;
			completeAction = null;
			waitTexture = null;
		}
		
		private void HandlePreProcess()
		{
			if(running || Disposed) return;
			running = true;
			waitAction?.Invoke(waitTexture);
			animations.ForEach(x => x?.Play());
		}

		private void HandleIntoProcess()
		{
			if(!running || Disposed) return;
			
			running = true;
			var picture = preloaderService.Get(url, preload, tokenSource.Token);
			picture.GetAwaiter().OnCompleted(() =>
			{
				completed = true;
				intoAction?.Invoke(picture.Result);
				completeAction?.Invoke();
				if (IsLayerProcess)
				{
					DisposeLayer();
					return;
				}

				Dispose();
			});
		}

		private Texture2D GetCopy(Texture2D original, bool makeCopy)
		{
			if (!makeCopy || !IsLayerProcess || !original || !original.isReadable) return original;
			var copyTexture = new Texture2D(original.width, original.height);
			copyTexture.SetPixels(original.GetPixels());
			copyTexture.Apply();
			return copyTexture;
		}
	}
}
