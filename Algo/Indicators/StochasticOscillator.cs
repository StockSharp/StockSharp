#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: StochasticOscillator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// The stochastic oscillator.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/stochastic_oscillator.html
	/// </remarks>
	[DisplayName("Stochastic Oscillator")]
	[Description("Stochastic Oscillator")]
	[Doc("topics/api/indicators/list_of_indicators/stochastic_oscillator.html")]
	public class StochasticOscillator : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StochasticOscillator"/>.
		/// </summary>
		public StochasticOscillator()
		{
			AddInner(K = new());
			AddInner(D = new() { Length = 3 });

			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

		/// <summary>
		/// %K.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.KKey,
			Description = LocalizedStrings.KKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public StochasticK K { get; }

		/// <summary>
		/// %D.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DKey,
			Description = LocalizedStrings.DKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage D { get; }

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $" %K={K.Length} %D={D.Length}";
	}
}