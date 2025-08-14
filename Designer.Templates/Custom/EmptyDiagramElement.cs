namespace StockSharp.Designer;

using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Diagram;
using StockSharp.Diagram.Elements;

/// <summary>
/// Sample diagram element demonstrates input and output sockets usage.
/// 
/// https://doc.stocksharp.com/topics/designer/strategies/using_code/csharp/creating_your_own_cube.html
/// </summary>
public class EmptyDiagramElement : DiagramExternalElement
{
	private readonly DiagramElementParam<int> _minValue;

	public EmptyDiagramElement()
	{
		// example property to show how to make parameters
	
		_minValue = AddParam("MinValue", 10)
			.SetBasic(true) // make parameter visible in basic mode
			.SetDisplay("Parameters", "Min value", "Min value parameter description", 10);
	}

	// output sockets are events marked with DiagramExternal attribute

	[DiagramExternal]
	public event Action<Unit> Output1;

	[DiagramExternal]
	public event Action<Unit> Output2;

	// uncomment to get Process method called every time when new arg received
	// (no need wait when all input args received)
	//public override bool WaitAllInput => false;

	// input sockets are method parameters marked with DiagramExternal attribute

	[DiagramExternal]
	public void Process(CandleMessage candle, Unit diff)
	{
		var res = candle.ClosePrice + diff;

		if (diff >= _minValue.Value)
			Output1?.Invoke(res);
		else
			Output2?.Invoke(res);
	}

	public override void Start()
	{
		base.Start();

		// add logic before start
	}

	public override void Stop()
	{
		base.Stop();

		// add logic after stop
	}

	public override void Reset()
	{
		base.Reset();

		// add logic for reset internal state
	}
}