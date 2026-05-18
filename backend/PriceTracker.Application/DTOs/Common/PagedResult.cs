namespace PriceTracker.Application.DTOs.Common;

public class PagedResult<T>
{
    public IEnumerable<T> Content       { get; set; } = new List<T>();
    public int            Page          { get; set; }
    public int            Size          { get; set; }
    public long           TotalElements { get; set; }
    public int            TotalPages    { get; set; }
    public bool           Last          { get; set; }

    public static PagedResult<T> From(IEnumerable<T> content, int page, int size, long totalElements) => new()
    {
        Content       = content,
        Page          = page,
        Size          = size,
        TotalElements = totalElements,
        TotalPages    = (int)Math.Ceiling(totalElements / (double)size),
        Last          = (page + 1) * size >= totalElements
    };
}