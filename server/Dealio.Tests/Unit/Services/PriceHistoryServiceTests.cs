namespace Dealio.Tests.Unit.Services;

using Moq;
using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.PriceHistory;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Services;

public sealed class PriceHistoryServiceTests
{
    [Fact]
    public async Task GetByListingIdAsync_ClampsPageAndPageSizeBeforeQuery()
    {
        var listingId = Guid.NewGuid();
        PaginationRequest? queried = null;
        var history = new Mock<IPriceHistoryRepository>();
        history.Setup(x => x.GetByListingIdPageAsync(listingId, It.IsAny<PriceHistoryFilterRequest>(), It.IsAny<PaginationRequest>()))
            .Callback<Guid, PriceHistoryFilterRequest, PaginationRequest>((_, _, pagination) => queried = new PaginationRequest { Page = pagination.Page, Size = pagination.Size })
            .ReturnsAsync((Array.Empty<Dealio.Domain.Entities.PriceHistory>(), 0L));

        var service = new PriceHistoryService(history.Object, Mock.Of<IListingRepository>());
        var result = await service.GetByListingIdAsync(listingId, new PriceHistoryFilterRequest(), new PaginationRequest { Page = -5, Size = 5000 });

        Assert.Equal(0, queried!.Page);
        Assert.Equal(200, queried.Size);
        Assert.Equal(0, result.TotalElements);
    }
}
