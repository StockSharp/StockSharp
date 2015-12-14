#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: StochasticOscillator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The stochastic oscillator.
	/// </summary>
	[DisplayName("Stochastic Oscillator")]
	[Description("Stochastic Oscillator")]
	public class StochasticOscillator : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StochasticOscillator"/>.
		/// </summary>
		public StochasticOscillator()
		{
			InnerIndicators.Add(K = new StochasticK());
			InnerIndicators.Add(D = new SimpleMovingAverage { Length = 3 });

			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// %K.
		/// </summary>
		[ExpandableObject]
		[DisplayName("%K")]
		[Description("%K")]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public StochasticK K { get; private set; }

		/// <summary>
		/// %D.
		/// </summary>
		[ExpandableObject]
		[DisplayName("%D")]
		[Description("%D")]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage D { get; private set; }
	}
}