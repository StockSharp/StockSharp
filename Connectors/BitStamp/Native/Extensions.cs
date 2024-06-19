namespace StockSharp.BitStamp.Native;

static class Extensions
{
    public static Sides ToSide(this int type)
    {
        return type == 0 ? Sides.Buy : Sides.Sell;
    }

    public static string ToCurrency(this SecurityId securityId)
    {
        return securityId.SecurityCode?.Remove("/").ToLowerInvariant();
    }

    public static SecurityId ToStockSharp(this string currency, bool format = true)
    {
        if (format)
        {
            if (currency.Length > 3 && !currency.Contains('/'))
                currency = currency.Insert(3, "/");

            currency = currency.ToUpperInvariant();
        }

        return new SecurityId
        {
            SecurityCode = currency,
            BoardCode = BoardCodes.BitStamp,
        };
    }

    public static DateTimeOffset ToDto(this string value, string format = "yyyy-MM-dd HH:mm:ss")
    {
        return value.ToDateTime(format).ApplyUtc();
    }
}