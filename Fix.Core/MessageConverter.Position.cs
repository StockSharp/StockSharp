namespace StockSharp.Fix;

/// <summary>
/// Position message converters: RequestForPositions.
/// </summary>
partial class MessageConverter
{
	/// <inheritdoc />
	public virtual PortfolioLookupMessage ToMessage(FixRequestForPositions fix)
	{
		var msg = new PortfolioLookupMessage
		{
			TransactionId = fix.PosReqId.ToLong(),
			PortfolioName = fix.Account,
			IsSubscribe = GetIsSubscribe(fix.SubscriptionRequestType),
			IsIncremental = fix.IsIncremental ?? false,
			UserId = fix.UserId,
		};

		// PortfolioMessage carries only ClientCode, so only PartyRole.ClientId is read.
		if (fix.Parties != null)
		{
			foreach (var party in fix.Parties)
			{
				if (party is null)
					continue;

				switch (party.PartyRole)
				{
					case PartyRole.ClientId:
						msg.ClientCode = party.PartyId;
						break;
				}
			}
		}

		if (!fix.ResponseId.IsEmpty())
			msg.OriginalTransactionId = fix.ResponseId.ToLong();

		if (fix.SecurityIds != null && fix.SecurityIds.Length > 0)
			msg.SecurityIds = fix.SecurityIds;

		return msg;
	}

	/// <inheritdoc />
	public virtual FixRequestForPositions ToFixRequestForPositions(PortfolioLookupMessage message)
	{
		return new FixRequestForPositions(
			PosReqId: FixId.FromLong(message.TransactionId),
			Account: message.PortfolioName,
			SubscriptionRequestType: ToFixSubscriptionType(message.IsSubscribe),
			ResponseId: message.OriginalTransactionId != 0 ? FixId.FromLong(message.OriginalTransactionId) : default,
			Side: null,
			IsIncremental: message.IsIncremental ? true : null,
			UserId: message.UserId,
			SecurityIds: message.SecurityIds,
			// ClientCode travels in the Parties block under PartyRole.ClientId.
			// PortfolioMessage carries no BrokerCode, hence the single-party array.
			Parties: BuildParties(message.ClientCode, brokerCode: null));
	}
}
