namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for News FIX message.
/// </summary>
/// <param name="MdResponseId">Response identifier.</param>
/// <param name="NewsMsg">News message.</param>
public record struct FixNews(
	string MdResponseId,
	NewsMessage NewsMsg);
