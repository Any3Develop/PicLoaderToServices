using System.Threading;
using System.Threading.Tasks;

namespace Services.DownloaderService
{
	public interface IDownloader<T>
	{
		Task<T> Download(string url, CancellationToken token = default);
	}
}