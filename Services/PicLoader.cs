using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RR.Core.DebugSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// PicLoader - A Run-Time image downloading and caching library.
/// Ex.
/// PicLoader.Init()
///		.Set(artUrl)
///		.SetCached(true)
///		.Into(cardArt)
///		.SetFadeTime(0f)
///		.SetLoadingPlaceholder(placeholderTexture)
///		.Run();
/// </summary>
public class PicLoader : MonoBehaviour
{
	private static readonly string filePath = Application.persistentDataPath + "/" + "PicLoader" + "/";

	private static bool ENABLE_GLOBAL_LOGS = true;

	private bool enableLog = false;
	private float fadeTime = 1;
	private bool cached = true;
	private int timeout = 30;
	private int timeoutAttempts = 3;

	//Loading YoYo fade
	private float fromAlpha = 0.8f;
	private float toAlpha = 0.2f;
	private float loadingFadeTime;
	private float initialAlpha;

	private enum RendererType
	{
		None,
		UiImage,
		Renderer,
		RawImage
	}

	private RendererType rendererType = RendererType.None;
	private GameObject targetObj;
	private string url = null;

	private Texture2D loadingPlaceholder, errorPlaceholder;

	private UnityAction onStartAction,
		onDownloadedAction,
		onLoadedAction,
		onEndAction;

	private UnityAction<int> onDownloadProgressChange;
	private UnityAction<string> onErrorAction;

	private static Dictionary<string, PicLoader> underProcess
		= new Dictionary<string, PicLoader>();

	private string uniqueHash;
	private int progress;

	/// <summary>
	/// Get instance of picLoader class
	/// </summary>
	public static PicLoader Init()
	{
		return new GameObject("PicLoader").AddComponent<PicLoader>();
	}

	/// <summary>
	/// Set image url for download.
	/// </summary>
	/// <param name="url">Image Url</param>
	/// <returns></returns>
	public PicLoader Set(string url)
	{
		if (enableLog)
			RRLogger.Log("Url set : " + url);

		this.url = url;
		return this;
	}

	/// <summary>
	/// Set fading animation time.
	/// </summary>
	/// <param name="fadeTime">Fade animation time. Set 0 for disable fading.</param>
	/// <returns></returns>
	public PicLoader SetFadeTime(float fadeTime)
	{
		if (enableLog)
			RRLogger.Log("Fading time set : " + fadeTime);

		this.fadeTime = fadeTime;
		return this;
	}

	/// <summary>
	/// Set loading YoYo fade animation. By default if enabled yoyo will restored initial alpha
	/// </summary>
	/// <param name="fadeTime">Fade animation time. Set 0 for disable fading.</param>
	/// <param name="fromAlpha">Start value</param>
	/// <param name="toAlpha">End value</param>
	/// <returns></returns>
	public PicLoader SetLoadingYoYoFadeTime(float fadeTime = 1f, float fromAlpha = 0.2f, float toAlpha = 0.8f)
	{
		if (enableLog)
			RRLogger.Log("Fading time set : " + fadeTime);

		this.loadingFadeTime = Mathf.Max(0,fadeTime);
		this.fromAlpha = Mathf.Clamp01(fromAlpha);
		this.toAlpha = Mathf.Clamp01(toAlpha);
		return this;
	}
	
	/// <summary>
	/// Set target Image component.
	/// </summary>
	/// <param name="image">target Unity UI image component</param>
	/// <returns></returns>
	public PicLoader Into(Image image)
	{
		if (enableLog)
			RRLogger.Log("Target as UIImage set : " + image);

		rendererType = RendererType.UiImage;
		this.targetObj = image.gameObject;
		initialAlpha = image.color.a;
		return this;
	}

	/// <summary>
	/// Set target Renderer component.
	/// </summary>
	/// <param name="renderer">target renderer component</param>
	/// <returns></returns>
	public PicLoader Into(Renderer renderer)
	{
		if (enableLog)
			RRLogger.Log("Target as Renderer set : " + renderer);

		rendererType = RendererType.Renderer;
		this.targetObj = renderer.gameObject;
		if(renderer.material.HasProperty("_Color"))
			initialAlpha = renderer.material.color.a;
		else
			initialAlpha = 1;
		return this;
	}

	public PicLoader Into(RawImage rawImage)
	{
		if (enableLog)
			RRLogger.Log("Target as RawImage set : " + rawImage);

		rendererType = RendererType.RawImage;
		this.targetObj = rawImage.gameObject;
		initialAlpha = rawImage.color.a;
		return this;
	}

	#region Actions

	public PicLoader OnStart(UnityAction action)
	{
		this.onStartAction = action;

		if (enableLog)
			RRLogger.Log("On start action set : " + action);

		return this;
	}

	public PicLoader OnDownloaded(UnityAction action)
	{
		this.onDownloadedAction = action;

		if (enableLog)
			RRLogger.Log("On downloaded action set : " + action);

		return this;
	}

	public PicLoader OnDownloadProgressChanged(UnityAction<int> action)
	{
		this.onDownloadProgressChange = action;

		if (enableLog)
			RRLogger.Log("On download progress changed action set : " + action);

		return this;
	}

	public PicLoader OnLoaded(UnityAction action)
	{
		this.onLoadedAction = action;

		if (enableLog)
			RRLogger.Log("On loaded action set : " + action);

		return this;
	}

	public PicLoader OnError(UnityAction<string> action)
	{
		this.onErrorAction = action;

		if (enableLog)
			RRLogger.Log("On error action set : " + action);

		return this;
	}

	public PicLoader OnEnd(UnityAction action)
	{
		this.onEndAction = action;

		if (enableLog)
			RRLogger.Log("On end action set : " + action);

		return this;
	}

	#endregion Actions

	/// <summary>
	/// Show or hide logs in console.
	/// </summary>
	/// <param name="enable">'true' for show logs in console.</param>
	/// <returns></returns>
	public PicLoader SetEnableLog(bool enable)
	{
		this.enableLog = enable;

		if (enable)
			RRLogger.Log("Logging enabled : true");

		return this;
	}

	/// <summary>
	/// Set the sprite of image when picLoader is downloading and loading image
	/// </summary>
	/// <param name="placeholder">loading texture</param>
	/// <returns></returns>
	public PicLoader SetLoadingPlaceholder(Texture2D placeholder)
	{
		this.loadingPlaceholder = placeholder;

		if (enableLog)
			RRLogger.Log("Loading placeholder has been set.");

		return this;
	}

	/// <summary>
	/// Set image sprite when some error occurred during downloading or loading image
	/// </summary>
	/// <param name="placeholder">error texture</param>
	/// <returns></returns>
	public PicLoader SetErrorPlaceholder(Texture2D placeholder)
	{
		this.errorPlaceholder = placeholder;

		if (enableLog)
			RRLogger.Log("Error placeholder has been set.");

		return this;
	}

	/// <summary>
	/// Enable cache
	/// </summary>
	/// <returns></returns>
	public PicLoader SetCached(bool cached)
	{
		this.cached = cached;

		if (enableLog)
			RRLogger.Log("Cache enabled : " + cached);

		return this;
	}

	/// <summary>
	/// Set timeout & connection attempts.
	/// </summary>
	/// <param name="timeout">Timeout in sec. Default is 30s.</param>
	/// <param name="attempts">Default is 3.</param>
	/// <returns></returns>
	public PicLoader SetTimeout(int timeout, int attempts)
	{
		this.timeout = timeout;
		this.timeoutAttempts = attempts;

		if (enableLog)
			RRLogger.Log($"$Timeout set : {timeout} sec & {timeoutAttempts} attempts");

		return this;
	}

	/// <summary>
	/// Start picLoader process.
	/// </summary>
	public void Run()
	{
		if (url == null)
		{
			Error("Url has not been set. Use 'Load' function to set image url.");
			return;
		}

		try
		{
			Uri uri = new Uri(url);
			this.url = uri.AbsoluteUri;
		}
		catch (Exception)
		{
			Error("Url is not correct.");
			return;
		}

		if (rendererType == RendererType.None || targetObj == null)
		{
			Error("Target has not been set. Use 'into' function to set target component.");
			return;
		}

		if (enableLog)
			RRLogger.Log("Start Working.");

		SetLoadingImage();
		onStartAction?.Invoke();

		if (!Directory.Exists(filePath))
			Directory.CreateDirectory(filePath);

		uniqueHash = CreateMD5(url);

		if (underProcess.ContainsKey(uniqueHash))
		{
			PicLoader sameProcess = underProcess[uniqueHash];
			sameProcess.onDownloadedAction += () =>
			{
				onDownloadedAction?.Invoke();

				LoadSpriteToImage();
			};
			return;
		}

		if (File.Exists(filePath + uniqueHash))
		{
			onDownloadedAction?.Invoke();
			LoadSpriteToImage();
			return;
		}

		underProcess.Add(uniqueHash, this);
		StopAllCoroutines();
		StartCoroutine(nameof(LoadingYoYoEffect));
		StartCoroutine(nameof(Downloader));
	}

	public void Abort()
	{
		StopAllCoroutines();
		if (targetObj != null && (loadingFadeTime > 0 || fadeTime > 0))
		{
			switch (rendererType)
			{
				case RendererType.Renderer:
					var renderer = targetObj.GetComponent<Renderer>();
					if(!renderer.material.HasProperty("_Color"))
						break;

					var colorRend = renderer.material.color;
					colorRend.a = initialAlpha;
					renderer.material.color = colorRend;
					break;

				case RendererType.UiImage:
					var image = targetObj.GetComponent<Image>();
					var colorImage = image.color;
					colorImage.a = initialAlpha;
					image.color = colorImage;
					break;

				case RendererType.RawImage:
					var rawImage = targetObj.GetComponent<RawImage>();
					var colorRawImage = rawImage.color;
					colorRawImage.a = initialAlpha;
					rawImage.color = colorRawImage;
					break;
			}
		}

		if (underProcess.ContainsKey(uniqueHash))
		{
			underProcess.Remove(uniqueHash);
		}
		
		Finish();
	}

	private IEnumerator Downloader()
	{
		if (enableLog)
			RRLogger.Log("Download started.");

		var attempts = 0;
		UnityWebRequest webRequest;
		do
		{
			webRequest = new UnityWebRequest(url)
			{
				timeout = timeout,
				downloadHandler = new DownloadHandlerBuffer()
			};
			webRequest.SendWebRequest();

			if (attempts++ > 0)
				RRLogger.Log($"504 Timeout error. Retrying... [attempt: {attempts}]");

			while (!webRequest.isDone)
			{
				if (webRequest.error != null)
				{
					Error("Error while downloading the image : " + webRequest.error);
					yield break;
				}

				progress = Mathf.FloorToInt(webRequest.downloadProgress * 100);
				onDownloadProgressChange?.Invoke(progress);

				if (enableLog)
					RRLogger.Log("Downloading progress : " + progress + "%");
				yield return null;
			}
		} while (!webRequest.isDone || webRequest.responseCode == 504 && attempts <= timeoutAttempts);

		if (webRequest.error == null)
			File.WriteAllBytes(filePath + uniqueHash, webRequest.downloadHandler.data);

		webRequest.Dispose();
		onDownloadedAction?.Invoke();

		LoadSpriteToImage();

		underProcess.Remove(uniqueHash);
	}

	private IEnumerator LoadingYoYoEffect()
	{
		if(loadingFadeTime <= 0)
			yield break;
		
		switch (rendererType)
		{
			case RendererType.Renderer:

				if (targetObj.TryGetComponent<Renderer>(out var renderer) && renderer.material.HasProperty("_Color"))
				{
					var material = renderer.material;
					var color = material.color;
					while (true)
					{
						color.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.PingPong((Time.time ) / loadingFadeTime, 1f));

						if (renderer != null)
							material.color = color;
						yield return null;
					}
				}
				break;

			case RendererType.UiImage:
			case RendererType.RawImage:
				if (targetObj.TryGetComponent<Graphic>(out var graphic))
				{
					var color2 = graphic.color;
					initialAlpha = color2.a;
					while (true)
					{
						color2.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.PingPong((Time.time ) / loadingFadeTime, 1f));
						if (graphic != null)
							graphic.color = color2;
						yield return null;
					}
				}
				break;
		}
	}

	private void LoadSpriteToImage()
	{
		progress = 100;
		onDownloadProgressChange?.Invoke(progress);

		if (enableLog)
			RRLogger.Log("Downloading progress : " + progress + "%");

		if (!File.Exists(filePath + uniqueHash))
		{
			Error("Loading image file has been failed.");
			return;
		}

		StopAllCoroutines();
		StartCoroutine(ImageLoader());
	}

	private void SetLoadingImage()
	{
		if(loadingPlaceholder == null)
			return;
		
		switch (rendererType)
		{
			case RendererType.Renderer:
				Renderer renderer = targetObj.GetComponent<Renderer>();
				renderer.material.mainTexture = loadingPlaceholder;
				break;

			case RendererType.UiImage:
				Image image = targetObj.GetComponent<Image>();
				Sprite sprite = Sprite.Create(loadingPlaceholder,
					new Rect(0, 0, loadingPlaceholder.width, loadingPlaceholder.height),
					new Vector2(0.5f, 0.5f));
				image.sprite = sprite;
				break;

			case RendererType.RawImage:
				RawImage rawImage = targetObj.GetComponent<RawImage>();
				rawImage.texture = loadingPlaceholder;
				break;
		}
	}

	private IEnumerator ImageLoader(Texture2D texture = null)
	{
		if (enableLog)
			RRLogger.Log("Start loading image.");

		if (texture == null)
		{
			byte[] fileData;
			fileData = File.ReadAllBytes(filePath + uniqueHash);
			texture = new Texture2D(2, 2);
			//ImageConversion.LoadImage(texture, fileData);
			texture.LoadImage(fileData); //..this will auto-resize the texture dimensions.
		}

		if (targetObj != null)
			switch (rendererType)
			{
				case RendererType.Renderer:
					yield return LoadRenderer();
					break;

				case RendererType.UiImage:
					yield return LoadUiImage();
					break;

				case RendererType.RawImage:
					yield return LoadRawImage();
					break;
			}

		onLoadedAction?.Invoke();

		if (enableLog)
			RRLogger.Log("Image has been loaded.");

		Finish();

		// Loaders
		IEnumerator LoadRenderer()
		{
			var renderer = targetObj.GetComponent<Renderer>();

			if (renderer == null || renderer.material == null)
				yield break;

			renderer.material.mainTexture = texture;
	
			if (renderer.material.HasProperty("_Color"))
			{
				var material = renderer.material;
				var color = material.color;
				var maxAlpha = loadingFadeTime <= 0 
					? color.a 
					: initialAlpha;
				if (fadeTime > 0)
				{
					color.a = 0;
					material.color = color;
					float time = Time.time;
					while (color.a < maxAlpha)
					{
						color.a = Mathf.Lerp(0, maxAlpha, (Time.time - time) / fadeTime);

						if (renderer != null)
							renderer.material.color = color;

						yield return null;
					}
				}
				else if(loadingFadeTime > 0)
				{
					color.a = maxAlpha;
					material.color = color;
				}
			}
		}

		IEnumerator LoadUiImage()
		{
			var image = targetObj.GetComponent<Image>();

			if (image == null)
				yield break;

			Sprite sprite = Sprite.Create(texture,
				new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

			image.sprite = sprite;
			var color = image.color;
			var maxAlpha = loadingFadeTime <= 0 
				? color.a 
				: initialAlpha;

			if (fadeTime > 0)
			{
				color.a = 0;
				image.color = color;

				float time = Time.time;
				while (color.a < maxAlpha)
				{
					color.a = Mathf.Lerp(0, maxAlpha, (Time.time - time) / fadeTime);

					if (image != null)
						image.color = color;
					yield return null;
				}
			}
			else if(loadingFadeTime > 0)
			{
				color.a = maxAlpha;
				image.color = color;
			}
		}

		IEnumerator LoadRawImage()
		{
			var rawImage = targetObj.GetComponent<RawImage>();

			if (rawImage == null)
				yield break;

			rawImage.texture = texture;
			var color = rawImage.color;
			var maxAlpha = loadingFadeTime <= 0 
				? color.a 
				: initialAlpha;

			if (fadeTime > 0)
			{
				color.a = 0;
				rawImage.color = color;

				float time = Time.time;
				while (color.a < maxAlpha)
				{
					color.a = Mathf.Lerp(0, maxAlpha, (Time.time - time) / fadeTime);

					if (rawImage != null)
						rawImage.color = color;
					yield return null;
				}
			}
			else if(loadingFadeTime > 0)
			{
				color.a = maxAlpha;
				rawImage.color = color;
			}
		}
	}

	private static string CreateMD5(string input)
	{
		// Use input string to calculate MD5 hash
		using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
		byte[] inputBytes = Encoding.ASCII.GetBytes(input);
		byte[] hashBytes = md5.ComputeHash(inputBytes);

		// Convert the byte array to hexadecimal string
		StringBuilder sb = new StringBuilder();
		foreach (var b in hashBytes)
			sb.Append(b.ToString("X2"));

		return sb.ToString();
	}

	private void Error(string message)
	{
		if (enableLog)
			RRLogger.Error("Error : " + message);

		onErrorAction?.Invoke(message);

		StopAllCoroutines();
		if (errorPlaceholder != null)
			StartCoroutine(ImageLoader(errorPlaceholder));
		else 
			Abort();
	}

	private void Finish()
	{
		if (enableLog)
			RRLogger.Log("Operation has been finished.");

		if (!cached)
		{
			try
			{
				File.Delete(filePath + uniqueHash);
			}
			catch (Exception ex)
			{
				if (enableLog)
					RRLogger.Error($"Error while removing cached file: {ex.Message}");
			}
		}

		onEndAction?.Invoke();

		Invoke(nameof(Destroyer), 0.5f);
	}

	private void Destroyer()
	{
		Destroy(gameObject);
	}

	/// <summary>
	/// Clear a certain cached file with its url
	/// </summary>
	/// <param name="url">Cached file url.</param>
	/// <returns></returns>
	public static void ClearCache(string url)
	{
		try
		{
			File.Delete(filePath + CreateMD5(url));

			if (ENABLE_GLOBAL_LOGS)
				RRLogger.Log($"Cached file has been cleared: {url}");
		}
		catch (Exception ex)
		{
			if (ENABLE_GLOBAL_LOGS)
				RRLogger.Warning($"Error while removing cached file: {ex.Message}");
		}
	}

	/// <summary>
	/// Clear all picLoader cached files
	/// </summary>
	/// <returns></returns>
	public static void ClearAllCachedFiles()
	{
		try
		{
			Directory.Delete(filePath, true);

			if (ENABLE_GLOBAL_LOGS)
				RRLogger.Log("All PicLoader cached files has been cleared.");
		}
		catch (Exception ex)
		{
			if (ENABLE_GLOBAL_LOGS)
				RRLogger.Warning($"Error while removing cached file: {ex.Message}");
		}
	}
}