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

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Acceleration / Deceleration Indicator.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/acceleration_deceleration.
	/// </remarks>
	[DisplayName("A/D")]
	[DescriptionLoc(LocalizedStrings.Str835Key)]
	public class Acceleration : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Acceleration"/>.
		/// </summary>
		public Acceleration()
			: this(new AwesomeOscillator(), new SimpleMovingAverage { Length = 5 })
		{
		}

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

		/// <summary>
		/// The moving average.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("MA")]
		[DescriptionLoc(LocalizedStrings.Str731Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage Sma { get; }

		/// <summary>
		/// Awesome Oscillator.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("AO")]
		[DescriptionLoc(LocalizedStrings.Str836Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AwesomeOscillator Ao { get; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Sma.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var aoValue = Ao.Process(input);

			if (Ao.IsFormed)
				return new DecimalIndicatorValue(this, aoValue.GetValue<decimal>() - Sma.Process(aoValue).GetValue<decimal>());

			return new DecimalIndicatorValue(this, aoValue.GetValue<decimal>());
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Sma.LoadNotNull(settings, nameof(Sma));
			Ao.LoadNotNull(settings, nameof(Ao));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue(nameof(Sma), Sma.Save());
			settings.SetValue(nameof(Ao), Ao.Save());
		}
	}
}