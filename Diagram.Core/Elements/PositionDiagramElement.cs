namespace StockSharp.Diagram.Elements;

/// <summary>
/// Position element (for security and money) for the specified portfolio.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PositionKey,
	Description = LocalizedStrings.PositionElementKey,
	GroupName = LocalizedStrings.PositionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/positions/current.html")]
public class PositionDiagramElement : DiagramElement
{
	private DiagramSocket _securityInput;
	private readonly DiagramSocket _portfolioInput;
	private readonly DiagramSocket _outputSocket;
	private DiagramSocket _positionOutput;

	private Security _security;
	private Portfolio _portfolio;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "83BCD64C-0F75-4572-BE49-3D771A456F76".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Money";

	private readonly DiagramElementParam<bool> _isMoney;

	private Security Security => IsMoney ? EntitiesExtensions.MoneySecurity : _security ?? Strategy.Security;
	private Portfolio Portfolio => _portfolio ?? Strategy.Portfolio;

	/// <summary>
	/// Money position.
	/// </summary>
	public bool IsMoney
	{
		get => _isMoney.Value;
		set => _isMoney.Value = value;
	}

	private readonly DiagramElementParam<bool> _showPosition;

	/// <summary>
	/// Show position socket.
	/// </summary>
	public bool ShowPosition
	{
		get => _showPosition.Value;
		set => _showPosition.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionDiagramElement"/>.
	/// </summary>
	public PositionDiagramElement()
	{
		ProcessNullValues = false;

		UpdateSecurityInputSocket(true);
		_portfolioInput = AddInput(StaticSocketIds.Portfolio, LocalizedStrings.Portfolio, DiagramSocketType.Portfolio, OnProcessPortfolio, index: 1);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Position, DiagramSocketType.Unit);

		_isMoney = AddParam<bool>(nameof(IsMoney))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Position, LocalizedStrings.Money, LocalizedStrings.MoneyPositionDesc, 30)
			.SetOnValueChangedHandler(value =>
			{
				UpdateSecurityInputSocket();
				RaisePropertiesChanged();
			});

		_showPosition = AddParam<bool>(nameof(ShowPosition))
			.SetDisplay(LocalizedStrings.Position, LocalizedStrings.ShowPositionSocket, LocalizedStrings.ShowPositionSocket, 40)
			.SetOnValueChangedHandler(value => UpdatePositionOutputSocket());

		UpdatePositionOutputSocket();
	}

	private void UpdateSecurityInputSocket(bool force = false)
	{
		if (_securityInput != null)
		{
			RemoveSocket(_securityInput);
			_securityInput = null;
		}

		if (force || !IsMoney)
			_securityInput = AddInput(GenerateSocketId("security"), LocalizedStrings.Security, DiagramSocketType.Security, dsv => OnProcessSecurity(dsv, default), index: 0);
	}

	private void UpdatePositionOutputSocket()
	{
		if (_positionOutput != null)
		{
			RemoveSocket(_positionOutput);
			_positionOutput = null;
		}

		if (_showPosition.Value)
			_positionOutput = AddOutput(GenerateSocketId("position"), LocalizedStrings.Position, DiagramSocketType.Unit, index: 1);
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		ResetFlushPriority();

		if (IsMoney)
		{
			var provider = (IPortfolioProvider)Strategy;

			provider.NewPortfolio += PortfolioChanged;
			provider.PortfolioChanged += PortfolioChanged;

			provider.Portfolios.ForEach(PortfolioChanged);

			if (GetNumConnections(_portfolioInput) == 0)
				FlushPriority = FlushNormal;
		}
		else
		{
			if (_securityInput is null || GetNumConnections(_securityInput) == 0)
				FlushPriority = FlushNormal;
		}

		OnSubscribe();

		base.OnStart(time);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		if (IsMoney)
		{
			var provider = (IPortfolioProvider)Strategy;

			provider.NewPortfolio -= PortfolioChanged;
			provider.PortfolioChanged -= PortfolioChanged;
		}

		OnUnSubscribe();

		base.OnStop();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_security = default;
		_portfolio = default;
	}

	private void OnSubscribe()
	{
		if (IsMoney)
			return;

		var provider = (IPositionProvider)Strategy;

		provider.NewPosition += PositionChanged;
		provider.PositionChanged += PositionChanged;
	}

	private void OnUnSubscribe()
	{
		if (IsMoney)
			return;

		var provider = (IPositionProvider)Strategy;

		provider.NewPosition -= PositionChanged;
		provider.PositionChanged -= PositionChanged;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		UpdateSecurityInputSocket();
		UpdatePositionOutputSocket();
	}

	private bool _isFlushing;

	/// <inheritdoc />
	public override void Flush(DateTimeOffset time)
	{
		if (_isFlushing)
			return;

		try
		{
			_isFlushing = true;

			if (IsMoney)
				PortfolioChanged(Portfolio);
			else
				OnProcessSecurity(null, time);
		}
		finally
		{
			_isFlushing = false;
		}
	}

	private void PositionChanged(Position position)
	{
		PositionChanged(position, null);
	}

	private void PositionChanged(Position position, DiagramSocketValue source)
	{
		if (Portfolio != null && position.Portfolio != Portfolio)
			return;

		if (!IsMoney && position.Security != Security)
			return;

		if (_positionOutput != null)
			RaiseProcessOutput(_positionOutput, position.LocalTime, position, source);

		if (position.CurrentValue == null)
			return;

		RaiseProcessOutput(_outputSocket, position.LocalTime, (Unit)position.CurrentValue.Value, source);
	}

	private void PortfolioChanged(Portfolio portfolio)
	{
		if (portfolio != Portfolio)
			return;

		if (_positionOutput != null)
			RaiseProcessOutput(_positionOutput, portfolio.LocalTime, portfolio);

		var v = portfolio.CurrentValue;

		if (v == null)
		{
			if (_positionOutput != null)
				Strategy.Flush(portfolio);

			return;
		}

		RaiseProcessOutput(_outputSocket, portfolio.LocalTime, (Unit)v.Value);
		Strategy.Flush(portfolio);
	}

	private void OnProcessSecurity(DiagramSocketValue value, DateTimeOffset time)
	{
		if (IsMoney)
			return;

		if (value != null)
			_security = value.GetValue<Security>();

		if (Security == null)
			return;

		var positions = ((IPositionProvider)Strategy).Positions.ToArray();

		if (positions.Length != 0)
			positions.ForEach(p => PositionChanged(p, value));
		else
			RaiseProcessOutput(_outputSocket, value?.Time ?? time, new Unit(0), value);
	}

	private void OnProcessPortfolio(DiagramSocketValue value)
	{
		_portfolio = value.GetValue<Portfolio>();
	}
}