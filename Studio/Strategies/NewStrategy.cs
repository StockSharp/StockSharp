#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Strategies.StrategiesPublic
File: NewStrategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Strategies;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	public class NewStrategy : Strategy
	{
		protected override void OnStarted()
		{
			this.AddInfoLog(LocalizedStrings.Str3281);
			base.OnStarted();
		}

		protected override void OnReseted()
		{
			this.AddInfoLog(LocalizedStrings.Str3282);
			base.OnReseted();
		}

		protected override void OnStopped()
		{
			this.AddInfoLog(LocalizedStrings.Str3283);
			base.OnStopped();
		}
	}
}