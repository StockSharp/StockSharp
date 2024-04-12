#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: AverageDirectionalIndex.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Welles Wilder Average Directional Index.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/adx.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AdxKey,
		Description = LocalizedStrings.AverageDirectionalIndexKey)]
	[Doc("topics/api/indicators/list_of_indicators/adx.html")]
	public class AverageDirectionalIndex : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AverageDirectionalIndex"/>.
		/// </summary>
		public AverageDirectionalIndex()
			: this(new DirectionalIndex { Length = 14 }, new WilderMovingAverage { Length = 14 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AverageDirectionalIndex"/>.
		/// </summary>
		/// <param name="dx">Welles Wilder Directional Movement Index.</param>
		/// <param name="movingAverage">Moving Average.</param>
		public AverageDirectionalIndex(DirectionalIndex dx, LengthIndicator<decimal> movingAverage)
			: base(dx, movingAverage)
		{
			Dx = dx;
			MovingAverage = movingAverage;
			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

		/// <summary>
		/// Welles Wilder Directional Movement Index.
		/// </summary>
		[Browsable(false)]
		public DirectionalIndex Dx { get; }

		/// <summary>
		/// Moving Average.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> MovingAverage { get; }

		/// <summary>
		/// Period length.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PeriodKey,
			Description = LocalizedStrings.IndicatorPeriodKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int Length
		{
			get => MovingAverage.Length;
			set
			{
				MovingAverage.Length = Dx.Length = value;
				Reset();
			}
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);
			Length = storage.GetValue<int>(nameof(Length));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);
			storage.SetValue(nameof(Length), Length);
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + " " + Length;
	}
}