namespace StockSharp.Fix;

partial class MessageConverter
{
	/// <inheritdoc />
	public IEnumerable<MarketDataMessage> ToMessages(FixMarketDataRequest fix)
		=> fix.ToMessages();

	/// <inheritdoc />
	public FixMarketDataRequest ToFixMarketDataRequest(MarketDataMessage message, FixId mdReqId, FixId mdResponseId)
	{
		char[] mdEntryTypes;
		string[] mdEntryArgs;

		if (message.DataType2 == DataType.MarketDepth)
		{
			mdEntryTypes = [MDEntryType.Bid, MDEntryType.Offer];
			mdEntryArgs = [null, null];
		}
		else
		{
			mdEntryTypes = [message.DataType2.ToFixMDType(out var arg)];
			mdEntryArgs = [arg];
		}

		return new FixMarketDataRequest(
			mdReqId,
			mdResponseId,
			message.GetSubscriptionType(),
			mdEntryTypes,
			mdEntryArgs,
			message.SecurityId == default ? null : [message.SecurityId],
			message.SecurityType == null ? null : [message.SecurityType.Value.ToFix()],
			message.MaxDepth,
			message.From,
			message.To,
			message.AllowBuildFromSmallerTimeFrame ? true : null,
			message.IsCalcVolumeProfile ? true : null,
			message.IsFinishedOnly ? true : null,
			message.IsRegularTradingHours,
			message.BuildMode != MarketDataBuildModes.LoadAndBuild ? (int)message.BuildMode : null,
			message.BuildFrom?.ToFixMDType(out _),
			message.BuildField?.ToFix(),
			message.Skip,
			message.Count,
			message.Fields?.ToArray());
	}
}
