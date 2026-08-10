namespace StockSharp.Fix;

/// <summary>
/// Default implementation of <see cref="IMessageConverter"/>.
/// Provides bidirectional conversion between FIX record structs and StockSharp messages.
/// </summary>
public partial class MessageConverter : IMessageConverter
{
	/// <summary>
	/// Get security type from FIX string code.
	/// </summary>
	protected virtual SecurityTypes? GetSecurityType(string fixSecurityType)
		=> fixSecurityType?.FromFixType();

	/// <summary>
	/// Convert security type to FIX string code.
	/// </summary>
	protected virtual string ToFixSecurityType(SecurityTypes? securityType)
		=> securityType?.ToFix();

	/// <summary>
	/// Get side from FIX char code.
	/// </summary>
	protected virtual Sides? GetSide(char? fixSide)
		=> fixSide?.FromFixSide();

	/// <summary>
	/// Convert side to FIX char code.
	/// </summary>
	protected virtual char? ToFixSide(Sides? side)
		=> side?.ToFix();

	/// <summary>
	/// Get time in force from FIX char code.
	/// </summary>
	protected virtual Messages.TimeInForce? GetTimeInForce(char? fixTif)
	{
		return fixTif switch
		{
			FixTimeInForce.GoodTillCancel => Messages.TimeInForce.PutInQueue,
			FixTimeInForce.Day => Messages.TimeInForce.PutInQueue,
			FixTimeInForce.GoodTillDate => Messages.TimeInForce.PutInQueue,
			FixTimeInForce.FillOrKill => Messages.TimeInForce.MatchOrCancel,
			FixTimeInForce.ImmediateOrCancel => Messages.TimeInForce.CancelBalance,
			_ => null
		};
	}

	/// <summary>
	/// Convert time in force to FIX char code.
	/// </summary>
	protected virtual char? ToFixTimeInForce(Messages.TimeInForce? tif)
		=> tif?.ToFix();

	/// <summary>
	/// Convert time in force to FIX char code, using <paramref name="tillDate"/> to
	/// distinguish GoodTillCancel / Day / GoodTillDate (all represented as PutInQueue).
	/// </summary>
	protected virtual char? ToFixTimeInForce(Messages.TimeInForce? tif, DateTime? tillDate)
		// One rule, stated once - see Extensions.ToFix. A caller that says nothing still says
		// nothing here, so the tag stays off the wire; everything else is encoded with its expiry.
		=> tif is null ? null : tif.ToFix(tillDate);

	/// <summary>
	/// Get TillDate based on FIX TimeInForce.
	/// </summary>
	protected virtual DateTime? GetTillDate(char? fixTif, DateTime? expiryDate)
	{
		return fixTif switch
		{
			FixTimeInForce.Day => Messages.Extensions.Today,
			FixTimeInForce.GoodTillDate => expiryDate,
			_ => null
		};
	}

	/// <summary>
	/// Convert subscription type to FIX char code.
	/// </summary>
	protected virtual char? ToFixSubscriptionType(bool? isSubscribe)
	{
		if (isSubscribe == null)
			return null;

		return isSubscribe.Value ? '1' : '2';
	}

	/// <summary>
	/// Decode a FIX <c>SubscriptionRequestType</c> (263) into <see cref="ISubscriptionMessage.IsSubscribe"/>.
	/// FIX defines <c>0=Snapshot</c>, <c>1=SnapshotPlusUpdates</c>, <c>2=DisablePrevious</c>. Only
	/// an explicit <c>2</c> is an unsubscribe; a snapshot request (<c>0</c>) — and an absent type —
	/// are subscribes.
	/// </summary>
	protected virtual bool GetIsSubscribe(char? subscriptionRequestType)
		=> subscriptionRequestType is null or '0' or '1';

	/// <summary>
	/// Build FIX Parties array from client code and broker code.
	/// </summary>
	protected static FixParty[] BuildParties(string clientCode, string brokerCode)
	{
		if (clientCode.IsEmpty() && brokerCode.IsEmpty())
			return null;

		var list = new List<FixParty>(2);

		if (!clientCode.IsEmpty())
			list.Add(new FixParty { PartyId = clientCode, PartyRole = PartyRole.ClientId });

		if (!brokerCode.IsEmpty())
			list.Add(new FixParty { PartyId = brokerCode, PartyRole = PartyRole.EnteringFirm });

		return list.ToArray();
	}

	/// <summary>
	/// Get position effect from FIX char code.
	/// </summary>
	protected virtual OrderPositionEffects? GetPositionEffect(char? positionEffect)
		=> positionEffect.ToPositionEffect();

	/// <summary>
	/// Convert position effect to FIX char code.
	/// </summary>
	protected virtual char? ToFixPositionEffect(OrderPositionEffects? effect)
	{
		return effect switch
		{
			OrderPositionEffects.Default => PositionEffect.Default,
			OrderPositionEffects.CloseOnly => PositionEffect.Close,
			OrderPositionEffects.OpenOnly => PositionEffect.Open,
			_ => null
		};
	}

	/// <summary>
	/// Get margin mode from FIX cash margin code.
	/// </summary>
	protected virtual MarginModes? GetMarginMode(char? cashMargin)
	{
		if (cashMargin == null)
			return null;

		return cashMargin.Value.ToMarginMode();
	}

	/// <summary>
	/// Convert margin mode to FIX cash margin code.
	/// </summary>
	protected virtual char? ToFixCashMargin(MarginModes? marginMode)
	{
		return marginMode switch
		{
			MarginModes.Cross => CashMargin.MarginOpen,
			MarginModes.Isolated => CashMargin.MarginClose,
			_ => null
		};
	}

	/// <summary>
	/// Get order type from FIX char code and apply to message.
	/// </summary>
	protected virtual OrderTypes? GetOrderType(char? ordType)
	{
		return ordType switch
		{
			OrdType.Limit => OrderTypes.Limit,
			OrdType.Market => OrderTypes.Market,
			OrdType.Stop or OrdType.StopLimit or OrdType.MarketIfTouched or OrdType.MarketOnClose => OrderTypes.Conditional,
			FixExtensions.StopTrailing or FixExtensions.TakeProfit or FixExtensions.TakeProfitTrailing => OrderTypes.Conditional,
			_ => null
		};
	}

	/// <summary>
	/// Convert order type to FIX char code.
	/// </summary>
	protected virtual char? ToFixOrderType(OrderTypes? orderType, OrderCondition condition)
	{
		return orderType switch
		{
			OrderTypes.Limit => OrdType.Limit,
			OrderTypes.Market => OrdType.Market,
			OrderTypes.Conditional when condition is not null && condition.Parameters.TryGetValue(nameof(FixOrderCondition.Type), out var typeObj) && typeObj is FixStopOrderTypes stopType => stopType switch
			{
				FixStopOrderTypes.TakeProfit => FixExtensions.TakeProfit,
				FixStopOrderTypes.TakeProfitTrailing => FixExtensions.TakeProfitTrailing,
				FixStopOrderTypes.StopLossTrailing => FixExtensions.StopTrailing,
				_ => ((IStopLossOrderCondition)condition).ActivationPrice == null ? OrdType.Stop : OrdType.StopLimit,
			},
			OrderTypes.Conditional when condition is IStopLossOrderCondition slc =>
				slc.IsTrailing
					? FixExtensions.StopTrailing
					: (slc.ActivationPrice == null ? OrdType.Stop : OrdType.StopLimit),
			OrderTypes.Conditional when condition is ITakeProfitOrderCondition tpc => tpc.ActivationPrice == null ? OrdType.Stop : OrdType.StopLimit,
			OrderTypes.Conditional => OrdType.Stop,
			_ => null
		};
	}
}
