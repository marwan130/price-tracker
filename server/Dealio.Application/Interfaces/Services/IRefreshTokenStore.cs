namespace Dealio.Application.Interfaces.Services;

public interface IRefreshTokenStore
{
    void Save(string refreshToken, Guid userId);
    Guid? Get(string refreshToken);
    void Revoke(string refreshToken);
    void RevokeAllForUser(Guid userId);
}
