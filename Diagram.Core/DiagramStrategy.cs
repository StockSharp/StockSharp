namespace StockSharp.Diagram;

/// <summary>
/// The strategy whose algorithm is presented in the form of a diagram.
/// </summary>
public class DiagramStrategy : Strategy, INotifyPropertiesChanged
{
	private readonly List<IDiagramElementParam> _customParameters = [];
	private readonly HashSet<IStrategyParam> _diagramParam = [];

	private CompositionDiagramElement _composition;
	private SettingsStorage _compositionSettings;

	/// <summary>
	/// The strategy diagram.
	/// </summary>
	[Browsable(false)]
	public CompositionDiagramElement Composition
	{
		get => _composition;
		set
		{
			var prev = _composition;

			if (prev == value)
				return;

			if (prev is not null)
				DisposeComposition(prev);

			_composition = value;

			if (value is not null)
				CreateComposition(value);
		}
	}

	private readonly StrategyParam<int> _overflowLimit;

	/// <summary>
	/// Max allowed elements per iteration to prevent stack overflow.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		GroupName = LocalizedStrings.SettingsKey,
		Name = LocalizedStrings.OverflowKey,
		Description = LocalizedStrings.OverflowLimitKey,
		Order = 301)]
	public int OverflowLimit
	{
		get => _overflowLimit.Value;
		set => _overflowLimit.Value = value;
	}

	/// <inheritdoc/>
	protected override TimeSpan? HistoryCalculated
	{
		get
		{
			static T? getMaxOrNull<T>(IEnumerable<T?> values)
				where T : struct, IComparable<T>
			{
				if (values is null)
					throw new ArgumentNullException(nameof(values));

				T max = default;

				foreach (var v in values)
				{
					if (v is null)
						return null;

					if (v.Value.CompareTo(max) > 0)
						max = v.Value;
				}

				return max;
			}

			var composition = Composition;

			var maxTf = getMaxOrNull(composition.FindAllElements<CandlesDiagramElement>().Select(e => e.DataType.Arg as TimeSpan?));
			if (maxTf is null)
				return null;

			var maxNumValuesToInitialize = composition.FindAllElements<IndicatorDiagramElement>().Select(e => e.Indicator?.NumValuesToInitialize ?? 0).Concat([1]).Max();

			var interval = maxTf.Value.Multiply(maxNumValuesToInitialize);

			return TimeSpan.FromDays(interval.TotalDays.Ceiling());
		}
	}

	/// <summary>
	/// The strategy diagram change event.
	/// </summary>
	public event Action<CompositionDiagramElement> CompositionChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="DiagramStrategy"/>.
	/// </summary>
	public DiagramStrategy()
	{
		_overflowLimit = Param(nameof(OverflowLimit), 100)
			.SetDisplay(LocalizedStrings.Overflow, LocalizedStrings.OverflowLimit, LocalizedStrings.Schema)
			.SetRange(1, int.MaxValue)
			.SetBasic(false);

		_overflowLimit.CanOptimize = false;
	}

	private static IEnumerable<IDiagramElementParam> GetOptimizedParams(CompositionDiagramElement composition)
		=> composition.Parameters.Where(p => p.CanOptimize && p.Type.CanOptimize());

	private class IgnoreRefresh { }

	private void RefreshParameters(CompositionDiagramElement composition)
	{
		if (Scope<IgnoreRefresh>.IsDefined)
			return;

		var toRemove = _diagramParam.ToSet();

		foreach (var p in GetOptimizedParams(composition))
		{
			try
			{
				if (!Parameters.TryGetValue(p.Name, out var sp))
				{
					sp = StrategyParamHelper.CreateParam(p.Type, p.Name);
					sp.Attributes.AddRange(p.Attributes);
					sp.Value = p.Value;

					_diagramParam.Add(sp);
					Parameters.Add(sp);
				}
				else
				{
					sp.Attributes.Clear();
					sp.Attributes.AddRange(p.Attributes);
					sp.Value = p.Value;
					toRemove.Remove(sp);
				}
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
		}

		foreach (var p in toRemove)
		{
			_diagramParam.Remove(p);
			Parameters.Remove(p);
		}
	}

	private void CreateComposition(CompositionDiagramElement composition)
	{
		if (composition is null)
			throw new ArgumentNullException(nameof(composition));

		if (_compositionSettings != null)
			composition.Load(_compositionSettings);

		composition.Changed += OnCompositionChanged;
		composition.PropertiesChanged += RaisePropertiesChanged;

		composition.CanAutoName = false;
		composition.Strategy = this;

		composition.Init(this);

		RefreshParameters(composition);

		CompositionChanged?.Invoke(composition);
	}

	private void DisposeComposition(CompositionDiagramElement composition)
	{
		if (composition is null)
			throw new ArgumentNullException(nameof(composition));

		composition.UnInit();
		composition.Parent = null;
		composition.Changed -= OnCompositionChanged;
		composition.PropertiesChanged -= RaisePropertiesChanged;

		_compositionSettings = composition.Save();
	}

	private void OnCompositionChanged()
	{
		var composition = Composition;
		if (composition is not null)
			RefreshParameters(composition);

		RaiseParametersChanged(nameof(Composition));
		RaisePropertiesChanged();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		var composition = Composition ?? throw new InvalidOperationException(LocalizedStrings.DiagramNotSet);

		if (composition.HasErrors)
			throw new InvalidOperationException(LocalizedStrings.DiagramContainsErrors);

		composition.SuspendUndoManager();

		using (new Scope<IgnoreRefresh>())
		{
			try
			{
				TrySetParameters(composition);
			}
			catch
			{
				TryResetParameters();
				throw;
			}

			foreach (var param in GetOptimizedParams(composition))
			{
				if (!Parameters.TryGetValue(param.Name, out var sParam) ||
					sParam.Value is null)
					continue;

				try
				{
					param.Value = sParam.Value;
				}
				catch (Exception ex)
				{
					LogError(ex);
				}
			}
		}

		composition.Prepare();

		base.OnStarted(time);

		this.SuspendRules(() =>
		{
			try
			{
				composition.Start(time);
			}
			catch (Exception)
			{
				StopComposition(composition);
				throw;
			}
		});
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		var composition = Composition;

		if (composition is not null)
			StopComposition(composition);

		ClearElementsState();
		ClearSocketsState();

		base.OnStopped();
	}

	private void TrySetParameters(CompositionDiagramElement composition)
	{
		if (composition is null)
			throw new ArgumentNullException(nameof(composition));

		composition
			.Parameters
			.ForEach(p =>
			{
				if (p.Type == typeof(Security) && p.Value == null)
				{
					var security = GetSecurity();

					_customParameters.Add(p);
					p.SetValueWithIgnoreOnSave(security);
				}
				else if (p.Type == typeof(Portfolio) && p.Value == null)
				{
					var portfolio = GetPortfolio();

					_customParameters.Add(p);
					p.SetValueWithIgnoreOnSave(portfolio);
				}
			});
	}

	private void TryResetParameters()
	{
		_customParameters
			.CopyAndClear()
			.ForEach(p => p.Value = null);
	}

	private void StopComposition(CompositionDiagramElement composition)
	{
		if (composition is null)
			throw new ArgumentNullException(nameof(composition));

		composition.Stop();
		TryResetParameters();

		Composition.ResumeUndoManager();
	}

	/// <summary>
	/// Flush non trigger (root) elements.
	/// </summary>
	public void Flush<TMessage>(TMessage message)
		where TMessage : IServerTimeMessage
	{
		Flush(message.ServerTime);
	}

	/// <summary>
	/// Flush non trigger (root) elements.
	/// </summary>
	public void Flush(DateTimeOffset time)
	{
		Composition.Flush(time);

		ClearElementsState();
		ClearSocketsState();
	}

	private void ClearSocketsState()
	{
		foreach (var socket in _changedSockets)
		{
			socket.Value = null;
		}

		_changedSockets.Clear();
	}

	private void ClearElementsState()
	{
		foreach (var element in _changedElements)
		{
			element.ClearSocketValues();
		}

		_changedElements.Clear();
	}

	private readonly HashSet<DiagramElement> _changedElements = [];
	private readonly HashSet<DiagramSocket> _changedSockets = [];

	internal void ClearStateRequired(DiagramElement element, bool need)
	{
		if (element == null)
			throw new ArgumentNullException(nameof(element));

		if (need)
			_changedElements.Add(element);
		else
			_changedElements.Remove(element);
	}

	internal void ClearStateRequired(DiagramSocket socket, bool need)
	{
		if (socket == null)
			throw new ArgumentNullException(nameof(socket));

		if (need)
			_changedSockets.Add(socket);
		else
			_changedSockets.Remove(socket);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		var composition = Composition;

		if (composition != null && !composition.HasErrors)
			composition.Reset();

		base.OnReseted();
	}

	private const string _compositionKey = "CompositionSettings";

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		var composition = Composition;

		if (composition != null)
			storage.SetValue(_compositionKey, _compositionSettings = composition.Save());
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		Composition?.LoadIfNotNull(storage, _compositionKey);

		base.Load(storage);
	}

	/// <inheritdoc />
	protected override void CopyTo(Strategy copy)
	{
		base.CopyTo(copy);

		((DiagramStrategy)copy).Composition = (CompositionDiagramElement)Composition.Clone();
	}

	#region INotifyPropertiesChanged

	/// <summary>
	/// The available properties change event.
	/// </summary>
	public event Action PropertiesChanged;

	/// <summary>
	/// To call the available properties change event.
	/// </summary>
	protected virtual void RaisePropertiesChanged()
	{
		PropertiesChanged?.Invoke();
	}

	#endregion

	// TODO
	//
	///// <inheritdoc/>
	//public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	//	=>	Composition
	//		.Parameters
	//		.Where(p => p.Type == typeof(Security))
	//		.Select(p =>
	//		{
	//			var security = p.Value != null ? (Security)p.Value : Security;

	//			return security ?? throw new InvalidOperationException(LocalizedStrings.SecurityNotSpecified);
	//		})
	//		.Concat(base.GetWorkingSecurities())
	//		.Distinct();

	/// <inheritdoc/>
	public override IEnumerable<Portfolio> GetWorkingPortfolios()
		=> Composition.FindPortfolios();

	/// <inheritdoc/>
	public override IEnumerable<IOrderBookSource> OrderBookSources
		=> Composition.FindMarketDepthPanels().Cast<IOrderBookSource>();

	/// <inheritdoc/>
	protected override void DisposeManaged()
	{
		Composition = null;

		base.DisposeManaged();
	}
}
