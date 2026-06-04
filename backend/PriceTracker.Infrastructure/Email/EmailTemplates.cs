namespace PriceTracker.Infrastructure.Email;

public static class EmailTemplates
{
    public static string PriceDropAlert(
        string  productName,
        string? variantSku,
        string  storeName,
        decimal triggeredPrice,
        decimal targetPrice,
        string  currencyCode,
        string  productUrl)
    {
        var variantLine = variantSku is not null
            ? $"<p><strong>Variant:</strong> {variantSku}</p>"
            : string.Empty;

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #2e7d32;">Price Drop Alert</h2>
                <p>Good news! A product you are tracking has dropped below your target price.</p>
                <hr/>
                <p><strong>Product:</strong> {productName}</p>
                {variantLine}
                <p><strong>Store:</strong> {storeName}</p>
                <p><strong>Current Price:</strong>
                    <span style="color: #2e7d32; font-size: 1.2em; font-weight: bold;">
                        {triggeredPrice:F2} {currencyCode}
                    </span>
                </p>
                <p><strong>Your Target:</strong> {targetPrice:F2} {currencyCode}</p>
                <hr/>
                <a href="{productUrl}"
                   style="background:#2e7d32;color:#fff;padding:12px 24px;
                          text-decoration:none;border-radius:4px;display:inline-block;">
                    View Product
                </a>
                <p style="color:#888;font-size:0.85em;margin-top:24px;">
                    You are receiving this email because you set a price alert on Smart Price Tracker.
                </p>
            </body>
            </html>
            """;
    }
}