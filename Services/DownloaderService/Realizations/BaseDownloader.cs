using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Services.DownloaderService
{
    public abstract class BaseDownloader<T> : IDownloader<T> where T : DownloadHandler
    {
        protected int Timeout;
        protected int TimeoutAttempts = 1;

        public virtual async Task<T> Download(string url, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return GetDownloadHandler();
            }
            
            var attempts = 0;
            UnityWebRequest request;
            do
            {
                request = new UnityWebRequest(url)
                {
                    timeout = Timeout,
                    downloadHandler = GetDownloadHandler()
                };
                request.SendWebRequest();
                attempts++;

                while (!request.isDone)
                {
                    if (request.error != null || token.IsCancellationRequested)
                    {
                        request.Abort();
                        return GetDownloadHandler();
                    }

                    await Task.Yield();
                }
            } while (!request.isDone || request.responseCode == 504 && attempts <= TimeoutAttempts);

            if (token.IsCancellationRequested)
            {
                return GetDownloadHandler();
            }
            
            return (T)request.downloadHandler;
        }

        protected abstract T GetDownloadHandler();
    }
}