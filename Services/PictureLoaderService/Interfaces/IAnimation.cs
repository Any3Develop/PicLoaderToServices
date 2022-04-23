using System;

namespace Services.PictureLoaderService
{
    public interface IAnimation : IDisposable
    {
        void Play();
    }
}