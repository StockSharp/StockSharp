namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Serialization;

	/// <summary>
	/// Полоса Боллинджера.
	/// </summary>
	public class BollingerBand : BaseIndicator<decimal>
	{
		private readonly LengthIndicator<decimal> _ma;
		private readonly StandardDeviation _dev;

		/// <summary>
		/// Создать <see cref="BollingerBand"/>.
		/// </summary>
		/// <param name="ma">Скользящая средняя.</param>
		/// <param name="dev">Стандартное отклонение.</param>
		public BollingerBand(LengthIndicator<decimal> ma, StandardDeviation dev)
		{
			if (ma == null)
				throw new ArgumentNullException("ma");

			if (dev == null)
				throw new ArgumentNullException("dev");

			_ma = ma;
			_dev = dev;
		}

		/// <summary>
		/// Ширина канала.
		/// </summary>
		public decimal Width { get; set; }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			return new DecimalIndicatorValue(this, _ma.GetCurrentValue() + (Width * _dev.GetCurrentValue()));
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Width = settings.GetValue<decimal>("Width");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Width", Width);
		}
	}
}