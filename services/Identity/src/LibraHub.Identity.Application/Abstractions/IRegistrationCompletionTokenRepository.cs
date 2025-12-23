using LibraHub.Identity.Domain.Tokens;

namespace LibraHub.Identity.Application.Abstractions;

public interface IRegistrationCompletionTokenRepository
{
    Task<RegistrationCompletionToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(RegistrationCompletionToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(RegistrationCompletionToken token, CancellationToken cancellationToken = default);
}

