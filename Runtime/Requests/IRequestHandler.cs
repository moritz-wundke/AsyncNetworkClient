using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetClient.Requests
{
    public interface IRequestHandler
    {
        Task<ResponseContext> SendAsync(
            RequestContext context, 
            CancellationToken cancellationToken);
    }
}