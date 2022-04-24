using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Services.PictureLoaderService
{
    public class AnimationYoYo : IAnimation
    {
        private float duration;
        private float fromAlpha;
        private float toAlpha;
        private float restoreAlpha;
        private Action<float> intoAction;
        private Action<float> disposeAction;
        
        public AnimationYoYo(MaskableGraphic placeHolder)
        {
	        if (!placeHolder)
	        {
		        Debug.LogError("PlaceHolder is missing");
		        return;
	        }

	        restoreAlpha = placeHolder.color.a;
	        void SetInto(float result)
	        {
		        if (!placeHolder || disposeAction == null) return;
		        
		        placeHolder.color = SetAlpha(placeHolder.color, result);
	        }

	        intoAction += SetInto;
	        disposeAction += SetInto;
        }
        
        public AnimationYoYo(Material placeHolder)
        {
	        if (!placeHolder)
	        {
		        Debug.LogError("PlaceHolder is missing");
		        return;
	        }

	        if (!placeHolder.HasProperty("_Color"))
	        {
		        Debug.LogError("PlaceHolder does no contains Shader param [_Color]");
		        return;
	        }
	        
	        restoreAlpha = placeHolder.color.a;
	        void SetInto(float result)
	        {
		        if (!placeHolder || disposeAction == null) return;
		        
		        placeHolder.color = SetAlpha(placeHolder.color, result);
	        }

	        intoAction += SetInto;
	        disposeAction += SetInto;
        }

        public AnimationYoYo Setup(float duration = 1f, 
                                   float fromAlpha = 0.5f, 
                                   float toAlpha = 0.8f, 
                                   float? restoreAlpha = null)
        {
	        this.duration = duration;
	        this.fromAlpha = fromAlpha;
	        this.toAlpha = toAlpha;
	        this.restoreAlpha = restoreAlpha ?? this.restoreAlpha;
	        return this;
        }

        public async void Play()
        {
            while (disposeAction != null)
            {
                intoAction?.Invoke(Mathf.Lerp(fromAlpha, toAlpha, Mathf.PingPong((Time.time) / duration, 1f)));
                await Task.Yield();
            }
        }

        public void Dispose()
        {
            if (disposeAction == null)
                return;

            disposeAction?.Invoke(restoreAlpha);
            disposeAction = null;
            intoAction = null;
        }

        private Color SetAlpha(Color value, float alpha)
        {
            value.a = alpha;
            return value;
        }
    }
}