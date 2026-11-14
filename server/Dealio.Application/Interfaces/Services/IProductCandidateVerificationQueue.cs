namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Products;

public interface IProductCandidateVerificationQueue
{
    Task<bool> StageAsync(Guid sessionId, ProductSearchResult candidate, CancellationToken cancellationToken = default, bool requireTitleMatch = true, bool deferVerification = false);
}
