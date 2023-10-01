namespace StockSharp.Algo.Strategies.Optimization;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Serialization;

using StockSharp.Algo.Strategies.Testing;
using StockSharp.Localization;

/// <summary>
/// Optimizer settings.
/// </summary>
public class OptimizerSettings : EmulationSettings
{
	private int _batchSize = Environment.ProcessorCount * 2;

	/// <summary>
	/// Number of simultaneously tested strategies.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ParallelKey,
		Description = LocalizedStrings.Str1419Key,
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

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(BatchSize), BatchSize)
			.Set(nameof(MaxIterations), MaxIterations)
		;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		BatchSize = storage.GetValue(nameof(BatchSize), BatchSize);
		MaxIterations = storage.GetValue(nameof(MaxIterations), MaxIterations);
	}
}
