namespace StockSharp.Algo.Indicators
{
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Индикатор, строящийся на основе маркет-данных.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.SecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str747Key)]
	public class Level1Indicator : BaseIndicator<decimal>
	{
		/// <summary>
		/// Поле маркет-данных первого уровня, которое используется как значение индикатора.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str748Key)]
		[DescriptionLoc(LocalizedStrings.Str749Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Level1Fields Field { get; set; }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var message = input.GetValue<Level1ChangeMessage>();

			var retVal = message.Changes.TryGetValue(Field);

			if (!IsFormed && retVal != null)
				IsFormed = true;

			return retVal == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, (decimal)retVal);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Field = settings.GetValue<Level1Fields>("Field");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Field", Field);
		}
	}
}