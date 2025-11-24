using System.Threading;
using System.Threading.Tasks;
using Arsis.Domain.Entities;

namespace Arsis.Application.Interfaces;

public interface IUserSessionRepository
{
    Task AddAsync(UserSession session, CancellationToken cancellationToken);
}
