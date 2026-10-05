using System.Threading;
using System.Threading.Tasks;

namespace Pos.Application.UseCases;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string plainPassword, CancellationToken cancellationToken = default);
}