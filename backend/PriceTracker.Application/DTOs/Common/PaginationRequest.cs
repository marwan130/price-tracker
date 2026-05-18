namespace PriceTracker.Application.DTOs.Common;

public class PaginationRequest
{
    public int    Page      { get; set; } = 0;
    public int    Size      { get; set; } = 20;
    public string Sort      { get; set; } = "CreatedAt";
    public string Direction { get; set; } = "DESC";
}