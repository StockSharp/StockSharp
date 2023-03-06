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

/// <summary>
/// Formula based implementation of <see cref="ICandlePattern"/>.
/// </summary>
public class CandlePattern : ICandlePattern
{
	private string[] _variables;
	private ExpressionFormula<bool> _formula;
	private bool _invalid;
	private bool _hasPrevVar;
	private ICandleMessage _prev;

	/// <summary>
	/// Formula.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.Str3115Key,
		Description = LocalizedStrings.Str3115Key,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public string Expression { get; set; }

	int ICandlePattern.CandlesCount => 1;

	void ICandlePattern.Validate()
	{
		if (Expression.IsEmpty())
			throw new InvalidOperationException("Expression is not set.");

		if (ServicesRegistry.TryCompiler is null)
			throw new InvalidOperationException($"Service {nameof(ICompiler)} is not initialized.");

		_formula = Expression.Compile<bool>();

		if (!_formula.Error.IsEmpty())
			throw new InvalidOperationException(_formula.Error);

		_variables = _formula.Variables.ToArray();
		_hasPrevVar = _variables.Any(v => v.StartsWithIgnoreCase("p"));
	}

	void ICandlePattern.Reset()
	{
		_prev = default;
		_formula = default;
		_variables = default;
		_hasPrevVar = default;
		_invalid = default;
	}

	bool ICandlePattern.Recognize(ICandleMessage candle)
	{
		if (_invalid)
			return false;

		if (_formula is null)
		{
			try
			{
				((ICandlePattern)this).Validate();
			}
			catch
			{
				_invalid = true;
				throw;
			}
		}

		try
		{
			if (_hasPrevVar && _prev is null)
				return false;

			return _formula.Calculate(_variables.Select(id =>
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

	string ICandlePattern.Name => Expression;

	void IPersistable.Load(SettingsStorage storage)
	{
		Expression = storage.GetValue<string>(nameof(Expression));
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.Set(nameof(Expression), Expression);
	}

	/// <inheritdoc />
	public override string ToString() => Expression;
}