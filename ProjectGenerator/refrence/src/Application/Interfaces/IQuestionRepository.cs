using Arsis.Domain.Entities;

namespace Arsis.Application.Interfaces;

public interface IQuestionRepository
{
    Task<IReadOnlyCollection<Question>> GetAllAsync(CancellationToken cancellationToken);
}
