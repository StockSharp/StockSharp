#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: MovingAverageConvergenceDivergenceSignal.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Convergence/divergence of moving averages with signal line.
	/// </summary>
	[DisplayName("MACD Signal")]
	[DescriptionLoc(LocalizedStrings.Str803Key)]
	public class MovingAverageConvergenceDivergenceSignal : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceSignal"/>.
		/// </summary>
		public MovingAverageConvergenceDivergenceSignal()
			: this(new MovingAverageConvergenceDivergence(), new ExponentialMovingAverage { Length = 9 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceSignal"/>.
		/// </summary>
		/// <param name="macd">Convergence/divergence of moving averages.</param>
		/// <param name="signalMa">Signaling Moving Average.</param>
		public MovingAverageConvergenceDivergenceSignal(MovingAverageConvergenceDivergence macd, ExponentialMovingAverage signalMa)
			: base(macd, signalMa)
		{
			Macd = macd;
			SignalMa = signalMa;
			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// Convergence/divergence of moving averages.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("MACD")]
		[DescriptionLoc(LocalizedStrings.Str797Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public MovingAverageConvergenceDivergence Macd { get; }

		/// <summary>
		/// Signaling Moving Average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.Str804Key)]
		[DescriptionLoc(LocalizedStrings.Str805Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage SignalMa { get; }
	}
}