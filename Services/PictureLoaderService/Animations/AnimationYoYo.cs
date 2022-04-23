using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Services.PictureLoaderService
{
    public class AnimationYoYo : IAnimation
    {
        private readonly float duration;
        private readonly float fromAlpha;
        private readonly float toAlpha;
        private Action<float> animationAction;
        private Action cancelAction;

        public AnimationYoYo(float duration = 1f,
                             float fromAlpha = 0.5f,
                             float toAlpha = 0.8f)
        {
            this.duration = duration;
            this.fromAlpha = fromAlpha;
            this.toAlpha = toAlpha;
        }

        public AnimationYoYo Into(Image placeHolder)
        {
            if (!placeHolder)
                return this;

            var restoreAlpha = placeHolder.color.a;
            animationAction += currentAlpha =>
            {
                if (!placeHolder)
                    return;

                placeHolder.color = SetAlpha(placeHolder.color, currentAlpha);
            };
            cancelAction += () =>
            {
                if (!placeHolder)
                    return;
                placeHolder.color = SetAlpha(placeHolder.color, restoreAlpha);
            };
            return this;
        }

        public AnimationYoYo Into(RawImage placeHolder)
        {
            if (!placeHolder)
                return this;

            var restoreAlpha = placeHolder.color.a;

            animationAction += currentAlpha =>
            {
                if (!placeHolder)
                    return;

                placeHolder.color = SetAlpha(placeHolder.color, currentAlpha);
            };
            cancelAction += () =>
            {
                if (!placeHolder)
                    return;
                placeHolder.color = SetAlpha(placeHolder.color, restoreAlpha);
            };
            return this;
        }

        public AnimationYoYo Into(Renderer placeHolder)
        {
            if (!placeHolder)
                return this;
            var material = placeHolder.material;
            if (!material.HasProperty("_Color"))
                return this;
            var restoreAlpha = material.color.a;
            animationAction += currentAlpha =>
            {
                if (!material)
                    return;
                material.color = SetAlpha(material.color, currentAlpha);
            };
            cancelAction += () =>
            {
                if (!material)
                    return;
                material.color = SetAlpha(material.color, restoreAlpha);
            };
            return this;
        }

        public async void Play()
        {
            while (cancelAction != null)
            {
                animationAction?.Invoke(Mathf.Lerp(fromAlpha, toAlpha, Mathf.PingPong((Time.time) / duration, 1f)));
                await Task.Yield();
            }
        }

        public void Dispose()
        {
            if (cancelAction == null)
                return;

            cancelAction?.Invoke();
            animationAction = null;
            cancelAction = null;
        }

        private Color SetAlpha(Color other, float alpha)
        {
            other.a = alpha;
            return other;
        }
    }
}