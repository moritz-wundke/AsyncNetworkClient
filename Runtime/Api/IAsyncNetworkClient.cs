using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetClient
{
    public interface IAsyncNetworkClient
    {
        Task<ResponseContext> SendAsync(HttpMethod method, string path, object request = null, CancellationToken cancellationToken = default);
    }
}