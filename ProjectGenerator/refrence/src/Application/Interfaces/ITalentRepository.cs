using Arsis.Domain.Entities;

namespace Arsis.Application.Interfaces;

public interface ITalentRepository
{
    Task<IReadOnlyCollection<Talent>> GetAllAsync(CancellationToken cancellationToken);
}
