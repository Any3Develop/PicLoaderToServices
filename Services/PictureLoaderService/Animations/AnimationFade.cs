using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Services.PictureLoaderService
{
    public class AnimationFade : IAnimation
    {
        private readonly float duration;
        private readonly float fromAlpha;
        private readonly float toAlpha;
        private Action<float> animationAction;
        private Action cancelAction;
        private Action completeAction;

        public AnimationFade(float duration = 1f,
                             float fromAlpha = 0f,
                             float toAlpha = 1f)
        {
            this.duration = duration;
            this.fromAlpha = fromAlpha;
            this.toAlpha = toAlpha;
        }

        public AnimationFade Into(Image placeHolder)
        {
            if (!placeHolder)
                return this;

            var restoreAlpha = placeHolder.color.a;
            animationAction += currentAlpha =>
            {
                if (!placeHolder || cancelAction == null)
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

        public AnimationFade Into(RawImage placeHolder)
        {
            if (!placeHolder)
                return this;

            var restoreAlpha = placeHolder.color.a;

            animationAction += currentAlpha =>
            {
                if (!placeHolder || cancelAction == null)
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

        public AnimationFade Into(Renderer placeHolder)
        {
            if (!placeHolder)
                return this;
            var material = placeHolder.material;
            if (!material.HasProperty("_Color"))
                return this;
            var restoreAlpha = material.color.a;
            animationAction += currentAlpha =>
            {
                if (!material || cancelAction == null)
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

        public AnimationFade OnComplete(Action onComplete)
        {
            completeAction = onComplete;
            return this;
        }

        public async void Play()
        {
            var time = Time.time;
            var currentAlpha = fromAlpha;
            while (cancelAction != null && currentAlpha < toAlpha)
            {
                animationAction?.Invoke(currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, (Time.time - time) / duration));
                await Task.Yield();
            }
            completeAction?.Invoke();
        }

        public void Dispose()
        {
            if (cancelAction == null)
                return;

            cancelAction?.Invoke();
            animationAction = null;
            cancelAction = null;
            completeAction = null;
        }

        private Color SetAlpha(Color other, float alpha)
        {
            other.a = alpha;
            return other;
        }
    }
}