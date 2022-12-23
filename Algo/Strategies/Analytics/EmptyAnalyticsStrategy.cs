namespace StockSharp.Studio.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;

	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Analytics;

	/// <summary>
	/// The empty analytic strategy.
	/// </summary>
	public class EmptyAnalyticsStrategy : BaseAnalyticsStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EmptyAnalyticsStrategy"/>.
		/// </summary>
		public EmptyAnalyticsStrategy()
		{
		}

		/// <summary>
		/// To analyze.
		/// </summary>
		protected override void OnAnalyze()
		{
			// notify the script stopped
			Stop();
		}
	}
}