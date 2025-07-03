using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetClient
{
    public interface IAsyncNetDecorator
    {
        public delegate Task<ResponseContext> NextDecorator(RequestContext context, CancellationToken cancellationToken);
        
        Task<ResponseContext> SendAsync(
            RequestContext context, 
            CancellationToken cancellationToken, 
            NextDecorator next);
    }
}