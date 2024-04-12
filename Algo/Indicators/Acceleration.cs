#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Acceleration.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Acceleration / Deceleration Indicator.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/a_d.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ADKey,
		Description = LocalizedStrings.AccDecIndicatorKey)]
	[Doc("topics/api/indicators/list_of_indicators/a_d.html")]
	public class Acceleration : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Acceleration"/>.
		/// </summary>
		public Acceleration()
			: this(new AwesomeOscillator(), new SimpleMovingAverage { Length = 5 })
		{
		}

		/// <inheritdoc />
		public override int NumValuesToInitialize => Math.Max(Ao.NumValuesToInitialize, Sma.NumValuesToInitialize);

		/// <summary>
		/// Initializes a new instance of the <see cref="Acceleration"/>.
		/// </summary>
		/// <param name="ao">Awesome Oscillator.</param>
		/// <param name="sma">The moving average.</param>
		public Acceleration(AwesomeOscillator ao, SimpleMovingAverage sma)
		{
			Ao = ao ?? throw new ArgumentNullException(nameof(ao));
			Sma = sma ?? throw new ArgumentNullException(nameof(sma));
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <summary>
		/// The moving average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.MAKey,
			Description = LocalizedStrings.MovingAverageKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage Sma { get; }

		/// <summary>
		/// Awesome Oscillator.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AOKey,
			Description = LocalizedStrings.AwesomeOscillatorKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public AwesomeOscillator Ao { get; }

		/// <inheritdoc />
		protected override bool CalcIsFormed() => Sma.IsFormed;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var aoValue = Ao.Process(input);

			if (Ao.IsFormed)
				return new DecimalIndicatorValue(this, aoValue.GetValue<decimal>() - Sma.Process(aoValue).GetValue<decimal>());

			return new DecimalIndicatorValue(this, aoValue.GetValue<decimal>());
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Sma.LoadIfNotNull(storage, nameof(Sma));
			Ao.LoadIfNotNull(storage, nameof(Ao));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Sma), Sma.Save());
			storage.SetValue(nameof(Ao), Ao.Save());
		}
	}
}
