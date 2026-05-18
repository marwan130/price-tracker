namespace PriceTracker.Application.DTOs.Common;

public class ApiError
{
    public string               Code      { get; set; } = string.Empty;
    public string               Message   { get; set; } = string.Empty;
    public IEnumerable<string>  Details   { get; set; } = new List<string>();
}