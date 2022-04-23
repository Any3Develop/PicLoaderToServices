using UnityEngine.Networking;

namespace Services.DownloaderService
{
    public class DefaultDownloader : BaseDownloader<DownloadHandler>
    {
        public DefaultDownloader(int timeOut = 30,
                                 int timeOutAttempts = 3)
        {
            Timeout = timeOut;
            TimeoutAttempts = timeOutAttempts;
        }

        protected override DownloadHandler GetDownloadHandler()
        {
            return new DownloadHandlerBuffer();
        }
    }
}