namespace Dealio.Tests.Unit.Services;

using Moq;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Services;
using Dealio.Domain.Entities;

public sealed class UserServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetTrackingPausedAsync_PersistsPauseState(bool paused)
    {
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, IsActive = true };
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        repository.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

        var service = new UserService(repository.Object);
        var result = await service.SetTrackingPausedAsync(userId, paused);

        Assert.Equal(paused, result);
        Assert.Equal(paused, user.IsTrackingPaused);
        repository.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task RequestDeletionAsync_DeactivatesAndPausesImmediatelyWithGraceDate()
    {
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, IsActive = true, IsTrackingPaused = false };
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        repository.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);
        var service = new UserService(repository.Object, deletionGraceDays: 30);

        var scheduled = await service.RequestDeletionAsync(userId);

        Assert.False(user.IsActive);
        Assert.True(user.IsTrackingPaused);
        Assert.Equal(scheduled, user.DeletionScheduledAtUtc);
        Assert.InRange((scheduled - DateTime.UtcNow).TotalDays, 29.99, 30);
        repository.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task CancelDeletionAsync_RestoresPreviousPauseState()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId, IsActive = false, IsTrackingPaused = true,
            TrackingPausedBeforeDeletion = false,
            DeletionRequestedAtUtc = DateTime.UtcNow.AddHours(-1),
            DeletionScheduledAtUtc = DateTime.UtcNow.AddDays(29)
        };
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        repository.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);
        var service = new UserService(repository.Object);

        await service.CancelDeletionAsync(userId);

        Assert.True(user.IsActive);
        Assert.False(user.IsTrackingPaused);
        Assert.Null(user.DeletionScheduledAtUtc);
        Assert.Null(user.DeletionRequestedAtUtc);
    }
}
