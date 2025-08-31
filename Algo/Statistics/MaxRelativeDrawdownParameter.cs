namespace StockSharp.Algo.Statistics;

/// <summary>
/// Maximum relative equity drawdown during the whole period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RelativeDrawdownKey,
	Description = LocalizedStrings.MaxRelativeDrawdownKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 7
)]
public class MaxRelativeDrawdownParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="MaxRelativeDrawdownParameter"/>.
	/// </summary>
	public MaxRelativeDrawdownParameter()
		: base(StatisticParameterTypes.MaxRelativeDrawdown)
	{
	}

	private decimal _posPeak;     // maximum equity when >= 0
	private decimal? _negPeak;    // peak in negative zone (least negative seen), null until first negative

	/// <inheritdoc />
	public override void Reset()
	{
		_posPeak = 0m;
		_negPeak = null;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (pnl >= 0)
		{
			// positive domain, use standard relative drawdown from positive peak
			_posPeak = Math.Max(_posPeak, pnl);
			if (_posPeak > 0)
			{
				var rel = (_posPeak - pnl) / _posPeak;
				Value = Math.Max(Value, rel);
			}
			// reset negative peak as we moved to non-negative range (optional)
		}
		else
		{
			// negative domain
			if (_posPeak > 0)
			{
				// if there was a positive peak before, relate to it
				var rel = (_posPeak - pnl) / _posPeak;
				Value = Math.Max(Value, rel);
			}
			else
			{
				// only negatives so far: define negative peak and compute relative changes w.r.t. that peak
				if (_negPeak is null)
				{
					_negPeak = pnl; // first negative reading, no relative drawdown yet
				}
				else
				{
					// peak is the least negative equity encountered so far
					_negPeak = Math.Max(_negPeak.Value, pnl);
					var drop = _negPeak.Value - pnl; // (-1000) - (-1500) = 500
					if (_negPeak.Value != 0)
					{
						var rel = drop / Math.Abs(_negPeak.Value);
						Value = Math.Max(Value, rel);
					}
				}
			}
		}
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("PosPeak", _posPeak);
		storage.Set("NegPeak", _negPeak);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_posPeak = storage.GetValue<decimal>("PosPeak");
		_negPeak = storage.GetValue<decimal?>("NegPeak");
		base.Load(storage);
	}
}
