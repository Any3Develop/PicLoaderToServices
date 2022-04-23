using System.Threading;
using System.Threading.Tasks;

namespace Services.CacheService
{
	public interface ICacheWriter
	{
		Task Write(byte[] value, string id, CancellationToken token = default);
		Task Remove(string id, CancellationToken token = default);
		Task Clear(CancellationToken token = default);
	}
}