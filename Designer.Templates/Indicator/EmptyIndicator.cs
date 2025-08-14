namespace StockSharp.Designer;

using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;

/// <summary>
/// Sample indicator demonstrating to save and load parameters.
/// 
/// Changes input price on +20% or -20%.
/// 
/// See more examples https://github.com/StockSharp/StockSharp/tree/master/Algo/Indicators
/// 
/// Doc https://doc.stocksharp.com/topics/designer/strategies/using_code/csharp/create_own_indicator.html
/// </summary>
public class EmptyIndicator : BaseIndicator
{
	private int _change = 20;

	public int Change
	{
		get => _change;
		set
		{
			_change = value;
			Reset();
		}
	}

	private int _counter;
	// formed indicator received all necessary inputs for be available for trading
	private bool _isFormed;

	protected override bool CalcIsFormed() => _isFormed;

	public override void Reset()
	{
		base.Reset();

		_isFormed = default;
		_counter = default;
	}

	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		// every 10th call try return empty value
		if (RandomGen.GetInt(0, 10) == 0)
			return new DecimalIndicatorValue(this, input.Time);

		if (_counter++ == 5)
		{
			// for example, our indicator needs 5 inputs for become formed
			_isFormed = true;
		}

		var value = input.ToDecimal();

		// random change on +20% or -20% current value

		value += value * RandomGen.GetInt(-Change, Change) / 100.0m;

		return new DecimalIndicatorValue(this, value, input.Time)
		{
			// final value means that this value for the specified input
			// is not changed anymore (for example, for candles that changes with last price)
			IsFinal = RandomGen.GetBool()
		};
	}

	// persist our properties to save for further the app restarts

	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Change = storage.GetValue<int>(nameof(Change));
	}

	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Change), Change);
	}

	public override string ToString() => $"Change: {Change}";
}