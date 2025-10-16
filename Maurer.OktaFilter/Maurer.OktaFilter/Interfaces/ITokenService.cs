using Maurer.OktaFilter.Models;

namespace Maurer.OktaFilter.Interfaces
{
    public interface ITokenService
    {
        Task<OktaToken?> GetToken(CancellationToken cancellationToken = default);
    }
}
