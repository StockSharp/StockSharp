namespace StockSharp.Algo.Indicators
{
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The indicator, built on the market data basis.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.SecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str747Key)]
	public class Level1Indicator : BaseIndicator
	{
		/// <summary>
		/// Level one market-data field, which is used as an indicator value.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str748Key)]
		[DescriptionLoc(LocalizedStrings.Str749Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Level1Fields Field { get; set; }

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var message = input.GetValue<Level1ChangeMessage>();

			var retVal = message.Changes.TryGetValue(Field);

			if (!IsFormed && retVal != null && input.IsFinal)
				IsFormed = true;

			return retVal == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, (decimal)retVal);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Field = settings.GetValue<Level1Fields>("Field");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Field", Field);
		}
	}
}