#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: BollingerBands.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Bollinger Bands.
	/// </summary>
	[DisplayName("Bollinger")]
	[DescriptionLoc(LocalizedStrings.Str777Key)]
	public class BollingerBands : BaseComplexIndicator
	{
		private readonly StandardDeviation _dev = new StandardDeviation();

		/// <summary>
		/// Initializes a new instance of the <see cref="BollingerBands"/>.
		/// </summary>
		public BollingerBands()
			: this(new SimpleMovingAverage())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BollingerBands"/>.
		/// </summary>
		/// <param name="ma">Moving Average.</param>
		public BollingerBands(LengthIndicator<decimal> ma)
		{
			InnerIndicators.Add(MovingAverage = ma);
			InnerIndicators.Add(UpBand = new BollingerBand(MovingAverage, _dev) { Name = "UpBand" });
			InnerIndicators.Add(LowBand = new BollingerBand(MovingAverage, _dev) { Name = "LowBand" });
			Width = 2;
		}

		/// <summary>
		/// Middle line.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> MovingAverage { get; }

		/// <summary>
		/// Upper band +.
		/// </summary>
		[Browsable(false)]
		public BollingerBand UpBand { get; }

		/// <summary>
		/// Lower band -.
		/// </summary>
		[Browsable(false)]
		public BollingerBand LowBand { get; }

		/// <summary>
		/// Period length. By default equal to 1.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str778Key)]
		[DescriptionLoc(LocalizedStrings.Str779Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual int Length
		{
			get => MovingAverage.Length;
			set
			{
				_dev.Length = MovingAverage.Length = value;
				Reset();
			}
		}

		/// <summary>
		/// Bollinger Bands channel width. Default value equal to 2.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str780Key)]
		[DescriptionLoc(LocalizedStrings.Str781Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Width
		{
			get => UpBand.Width;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str782);

				UpBand.Width = value;
				LowBand.Width = -value;
 
				Reset();
			}
		}
		
		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_dev.Reset();
			//MovingAverage.Reset();
			//UpBand.Reset();
			//LowBand.Reset();
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => MovingAverage.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_dev.Process(input);
			var maValue = MovingAverage.Process(input);
			var value = new ComplexIndicatorValue(this);
			value.InnerValues.Add(MovingAverage, maValue);
			value.InnerValues.Add(UpBand, UpBand.Process(input));
			value.InnerValues.Add(LowBand, LowBand.Process(input));
			return value;
		}
	}
}