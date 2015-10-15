namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Acceleration / Decelration Indicator.
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
			if (ao == null)
				throw new ArgumentNullException("ao");

			if (sma == null)
				throw new ArgumentNullException("sma");

			Ao = ao;
			Sma = sma;
		}

		/// <summary>
		/// The moving average.
		/// </summary>
		[ExpandableObject]
		[DisplayName("MA")]
		[DescriptionLoc(LocalizedStrings.Str731Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage Sma { get; private set; }

		/// <summary>
		/// Awesome Oscillator.
		/// </summary>
		[ExpandableObject]
		[DisplayName("AO")]
		[DescriptionLoc(LocalizedStrings.Str836Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AwesomeOscillator Ao { get; private set; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed { get { return Sma.IsFormed; } }

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

			Sma.LoadNotNull(settings, "Sma");
			Ao.LoadNotNull(settings, "Ao");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("Sma", Sma.Save());
			settings.SetValue("Ao", Ao.Save());
		}
	}
}