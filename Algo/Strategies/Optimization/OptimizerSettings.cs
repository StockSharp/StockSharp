namespace StockSharp.Algo.Strategies.Optimization;

using StockSharp.Algo.Testing;

/// <summary>
/// Optimizer settings.
/// </summary>
public class OptimizerSettings : MarketEmulatorSettings
{
	private DateTime _startTime = DateTime.Today.AddYears(-1);

	/// <summary>
	/// Date in history for starting the paper trading.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use specified parameters in Start methods.")]
	public DateTime StartTime
	{
		get => _startTime;
		set
		{
			_startTime = value;
			NotifyPropertyChanged();
		}
	}

	private DateTime _stopTime = DateTime.Today;

	/// <summary>
	/// Date in history to stop the paper trading (date is included).
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use specified parameters in Start methods.")]
	public DateTime StopTime
	{
		get => _stopTime;
		set
		{
			_stopTime = value;
			NotifyPropertyChanged();
		}
	}

	private int _batchSize = Environment.ProcessorCount * 2;

	/// <summary>
	/// Number of simultaneously tested strategies.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ParallelKey,
		Description = LocalizedStrings.ParallelDescKey,
		GroupName = LocalizedStrings.OptimizationKey,
		Order = 200)]
	public int BatchSize
	{
		get => _batchSize;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			_batchSize = value;
			NotifyPropertyChanged();
		}
	}

	private int _maxIterations;

	/// <summary>
	/// Maximum possible iterations count. Zero means the option is ignored.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IterationsKey,
		Description = LocalizedStrings.MaxIterationsKey,
		GroupName = LocalizedStrings.OptimizationKey,
		Order = 201)]
	public int MaxIterations
	{
		get => _maxIterations;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			_maxIterations = value;
			NotifyPropertyChanged();
		}
	}

	/// <summary>
	/// <see cref="InMemoryMessageChannel.MaxMessageCount"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MaxMessagesKey,
		Description = LocalizedStrings.MaxMessagesDescKey,
		GroupName = LocalizedStrings.OptimizationKey,
		Order = 202)]
	public int MaxMessageCount { get; set; } = -1;

    /// <inheritdoc />
    public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
#pragma warning disable CS0618 // Type or member is obsolete
			.Set(nameof(StartTime), StartTime)
			.Set(nameof(StopTime), StopTime)
#pragma warning restore CS0618 // Type or member is obsolete
			.Set(nameof(BatchSize), BatchSize)
			.Set(nameof(MaxIterations), MaxIterations)
			.Set(nameof(MaxMessageCount), MaxMessageCount)
		;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

#pragma warning disable CS0618 // Type or member is obsolete
		StartTime = storage.GetValue(nameof(StartTime), StartTime);
		StopTime = storage.GetValue(nameof(StopTime), StopTime);
#pragma warning restore CS0618 // Type or member is obsolete
		BatchSize = storage.GetValue(nameof(BatchSize), BatchSize);
		MaxIterations = storage.GetValue(nameof(MaxIterations), MaxIterations);
		MaxMessageCount = storage.GetValue(nameof(MaxMessageCount), MaxMessageCount);
	}
}
