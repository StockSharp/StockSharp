namespace StockSharp.Diagram.Elements;

/// <summary>
/// The base class for order registering elements.
/// </summary>
public abstract class OrderRegisterBaseDiagramElement : OrderBaseDiagramElement
{
	private readonly DiagramElementParam<string> _clientCode;

	/// <summary>
	/// Client code assigned by the broker.
	/// </summary>
	public string ClientCode
	{
		get => _clientCode.Value;
		set => _clientCode.Value = value;
	}

	private readonly DiagramElementParam<string> _brokerCode;

	/// <summary>
	/// Broker firm code.
	/// </summary>
	public string BrokerCode
	{
		get => _brokerCode.Value;
		set => _brokerCode.Value = value;
	}

	private readonly DiagramElementParam<bool?> _isMarketMaker;

	/// <summary>
	/// Is the order of market-maker.
	/// </summary>
	public bool? IsMarketMaker
	{
		get => _isMarketMaker.Value;
		set => _isMarketMaker.Value = value;
	}

	private readonly DiagramElementParam<TimeInForce?> _timeInForce;

	/// <summary>
	/// Limit order time in force.
	/// </summary>
	public TimeInForce? TimeInForce
	{
		get => _timeInForce.Value;
		set => _timeInForce.Value = value;
	}

	private readonly DiagramElementParam<bool?> _isManual;

	/// <summary>
	/// Is order manual.
	/// </summary>
	public bool? IsManual
	{
		get => _isManual.Value;
		set => _isManual.Value = value;
	}

	private readonly DiagramElementParam<MarginModes?> _marginMode;

	/// <summary>
	/// Margin mode.
	/// </summary>
	public MarginModes? MarginMode
	{
		get => _marginMode.Value;
		set => _marginMode.Value = value;
	}

	private readonly DiagramElementParam<string> _comment;

	/// <summary>
	/// <see cref="Order.Comment"/>.
	/// </summary>
	public string Comment
	{
		get => _comment.Value;
		set => _comment.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderRegisterBaseDiagramElement"/> class.
	/// </summary>
	protected OrderRegisterBaseDiagramElement()
	{
		_timeInForce = AddParam(nameof(TimeInForce), (TimeInForce?)null)
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.TimeInForce, LocalizedStrings.LimitOrderTif, 70);

		_clientCode = AddParam<string>(nameof(ClientCode))
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.ClientCode, LocalizedStrings.ClientCodeDesc, 110);

		_brokerCode = AddParam<string>(nameof(BrokerCode))
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.Broker, LocalizedStrings.BrokerCode, 120);

		_marginMode = AddParam<MarginModes?>(nameof(MarginMode))
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.Margin, LocalizedStrings.MarginMode, 160);

		_isMarketMaker = AddParam(nameof(IsMarketMaker), (bool?)null)
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.MarketMaker, LocalizedStrings.MarketMakerOrder, 170);

		_isManual = AddParam(nameof(IsManual), (bool?)null)
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.Manual, LocalizedStrings.IsOrderManual, 180);

		_comment = AddParam(nameof(Comment), string.Empty)
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.Comment, LocalizedStrings.OrderComment, 56);
	}
}
