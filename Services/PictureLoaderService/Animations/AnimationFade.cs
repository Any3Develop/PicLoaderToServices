using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Services.PictureLoaderService
{
    public class AnimationFade : IAnimation
    {
	    private const float FLOAT_COMPARE_THRESHOLD = 0.01f;
	    private float duration;
        private float fromAlpha;
        private float toAlpha;
        private float endAlpha;
        
        private Action<float> intoAction;
        private Action<float> disposeAction;

        public AnimationFade(MaskableGraphic placeHolder)
        {
	        if (!placeHolder)
	        {
		        Debug.LogError("PlaceHolder is missing");
		        return;
	        }
	        
	        endAlpha = placeHolder.color.a;
	        void SetInto(float result)
	        {
		        if (!placeHolder) return;
		        
		        placeHolder.color = SetAlpha(placeHolder.color, result);
	        }
	        
	        intoAction += SetInto;
	        disposeAction += SetInto;
        }
        
        public AnimationFade(Material placeHolder)
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
	        
	        endAlpha = placeHolder.color.a;
	        void SetInto(float result)
	        {
		        if (!placeHolder) return;
		        
		        placeHolder.color = SetAlpha(placeHolder.color, result);
	        }
	        
	        intoAction += SetInto;
	        disposeAction += SetInto;
        }

        public AnimationFade Setup(float duration = 1f, 
                                   float fromAlpha = 0f, 
                                   float toAlpha = 1f, 
                                   float? endAlpha = null)
        {
	        this.duration = duration;
	        this.fromAlpha = fromAlpha;
	        this.toAlpha = toAlpha;
	        this.endAlpha = endAlpha ?? this.endAlpha;
	        return this;
        }
        
        public async void Play()
        {
            var time = Time.time;
            var currentAlpha = fromAlpha;
            while (disposeAction != null && Math.Abs(currentAlpha - toAlpha) < FLOAT_COMPARE_THRESHOLD)
            {
                intoAction?.Invoke(currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, (Time.time - time) / duration));
                await Task.Yield();
            }

            if (disposeAction != null)
	            intoAction?.Invoke(toAlpha);
        }

        public void Dispose()
        {
            if (disposeAction == null)
                return;
            
            disposeAction?.Invoke(endAlpha);
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