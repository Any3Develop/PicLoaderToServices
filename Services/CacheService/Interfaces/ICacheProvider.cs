using System.Threading;
using System.Threading.Tasks;

namespace Services.CacheService
{
	public interface ICacheProvider
	{
		Task<bool> Contains(string id, CancellationToken token = default);
		Task<byte[]> Get(string id, CancellationToken token = default);
	}
}