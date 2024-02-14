using Maurer.OktaFilter.Models;

namespace Maurer.OktaFilter.Interfaces
{
    public interface ITokenService
    {
        Task<Token?> GetToken();
    }
}
