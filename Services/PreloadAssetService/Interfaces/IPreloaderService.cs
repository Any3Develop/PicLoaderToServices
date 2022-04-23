using System.Threading;
using System.Threading.Tasks;

namespace Services.PreloadAssetService
{
	public interface IPreloaderService<T>
	{
		Task Preload(CancellationToken token = default);
		Task Unload(CancellationToken token = default);
		Task<T> Get(string url, bool preload = true, CancellationToken token = default);
	}
}