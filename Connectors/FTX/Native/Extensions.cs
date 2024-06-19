namespace StockSharp.FTX.Native;

static class Extensions
{
	/// <summary>
	/// Market side to S# side
	/// </summary>
	/// <param name="type">Side</param>
	/// <returns></returns>
	public static Sides ToSide(this string type)
	{
		return type == "buy" ? Sides.Buy : Sides.Sell;
	}

	/// <summary>
	/// Market order state to S# order state
	/// </summary>
	/// <param name="status">State</param>
	/// <returns></returns>
	public static OrderStates ToOrderState(this string status)
	{
		if (status == "new" || status == "open") return OrderStates.Active;
		return OrderStates.Done;
	}

	/// <summary>
	/// Market order type to S# order type
	/// </summary>
	/// <param name="type">Type</param>
	/// <returns></returns>
	public static OrderTypes ToOrderType(this string type)
	{
		return type == "market" ? OrderTypes.Market : OrderTypes.Limit;
	}

	/// <summary>
	/// Security ID to currency string
	/// </summary>
	/// <param name="securityId">ID</param>
	/// <returns></returns>
	public static string ToCurrency(this SecurityId securityId)
	{
		return securityId.SecurityCode;
	}

	/// <summary>
	/// Currency string to S# Security Id
	/// </summary>
	/// <param name="currency"></param>
	/// <returns></returns>
	public static SecurityId ToStockSharp(this string currency)
	{
		return new SecurityId
		{
			SecurityCode = currency,
			BoardCode = BoardCodes.FTX,
		};
	}
}