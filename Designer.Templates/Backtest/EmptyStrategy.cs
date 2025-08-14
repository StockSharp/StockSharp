namespace StockSharp.Designer;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Logging;
using Ecng.Drawing;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Charting;

/// <summary>
/// Empty strategy.
/// 
/// See more examples https://github.com/StockSharp/AlgoTrading
/// </summary>
public class EmptyStrategy : Strategy
{
	public EmptyStrategy()
	{
		_intParam = Param(nameof(IntParam), 80);
	}

	private readonly StrategyParam<int> _intParam;

	public int IntParam
	{
		get => _intParam.Value;
		set => _intParam.Value = value;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		LogInfo(nameof(OnStarted));

		base.OnStarted(time);
	}
}