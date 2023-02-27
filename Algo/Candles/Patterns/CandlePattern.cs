namespace StockSharp.Algo.Candles.Patterns;

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Compilation;
using Ecng.Compilation.Expressions;
using Ecng.Serialization;

using StockSharp.Localization;
using StockSharp.Messages;
using StockSharp.Logging;

/// <summary>
/// Formula based implementation of <see cref="ICandlePattern"/>.
/// </summary>
public class CandlePattern : ICandlePattern
{
	private readonly CachedSynchronizedList<string> _variables = new();
	private bool _hasPrevVar;
	private ICandleMessage _prev;

	private string _name = nameof(CandlePattern);

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.NameKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public string Name
	{
		get => _name;
		set => _name = value.ThrowIfEmpty(nameof(value));
	}

	/// <summary>
	/// Formula.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.Str3115Key,
		Description = LocalizedStrings.Str3115Key,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public string Expression
	{
		get => Formula.Expression;
		set
		{
			if (value.IsEmpty())
			{
				Formula = ExpressionFormula<bool>.CreateError(LocalizedStrings.ExpressionNotSet);
				return;
				//throw new ArgumentNullException(nameof(value));
			}

			if (ServicesRegistry.TryCompiler is not null)
			{
				Formula = value.Compile<bool>();

				_variables.Clear();

				if (Formula.Error.IsEmpty())
				{
					_variables.AddRange(Formula.Variables);

					_hasPrevVar = _variables.Cache.Any(v => v.StartsWithIgnoreCase("p"));
				}
				else
					new InvalidOperationException(Formula.Error).LogError();
			}
			else
				new InvalidOperationException($"Service {nameof(ICompiler)} is not initialized.").LogError();
		}
	}

	/// <summary>
	/// Compiled mathematical formula.
	/// </summary>
	public ExpressionFormula<bool> Formula { get; private set; } = ExpressionFormula<bool>.CreateError(LocalizedStrings.ExpressionNotSet);

	void ICandlePattern.Reset()
	{
		_prev = default;
	}

	bool ICandlePattern.Recognize(ICandleMessage candle)
	{
		try
		{
			if (_hasPrevVar && _prev is null)
				return false;

			return Formula.Calculate(_variables.Cache.Select(id =>
				(id?.ToUpperInvariant()) switch
				{
					"O" => candle.OpenPrice,
					"H" => candle.HighPrice,
					"L" => candle.LowPrice,
					"C" => candle.ClosePrice,
					"V" => candle.TotalVolume,
					"OI" => candle.OpenInterest ?? default,
					"B" => candle.GetBody(),
					"LEN" => candle.GetLength(),
					"TS" => candle.GetTopShadow(),
					"BS" => candle.GetBottomShadow(),

					"PO" => _prev.OpenPrice,
					"PH" => _prev.HighPrice,
					"PL" => _prev.LowPrice,
					"PC" => _prev.ClosePrice,
					"PV" => _prev.TotalVolume,
					"POI" => _prev.OpenInterest ?? default,
					"PB" => _prev.GetBody(),
					"PLEN" => _prev.GetLength(),
					"PTS" => _prev.GetTopShadow(),
					"PBS" => _prev.GetBottomShadow(),

					_ => throw new ArgumentOutOfRangeException(id),
				}).ToArray());
		}
		finally
		{
			_prev = candle;
		}
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		Name = storage.GetValue<string>(nameof(Name));
		Expression = storage.GetValue<string>(nameof(Expression));
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Name), Name)
			.Set(nameof(Expression), Expression)
		;
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}