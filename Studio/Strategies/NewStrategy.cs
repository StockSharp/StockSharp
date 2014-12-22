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