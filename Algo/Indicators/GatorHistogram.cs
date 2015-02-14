namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Serialization;

	/// <summary>
	/// Гистограмма осцилятора <see cref="GatorOscillator"/>.
	/// </summary>
	public class GatorHistogram : BaseIndicator<decimal>
	{
		private readonly AlligatorLine _line1;
		private readonly AlligatorLine _line2;
		private readonly bool _isNegative;

		internal GatorHistogram(AlligatorLine line1, AlligatorLine line2, bool isNegative)
		{
			if (line1 == null)
				throw new ArgumentNullException("line1");

			if (line2 == null)
				throw new ArgumentNullException("line2");

			_line1 = line1;
			_line2 = line2;
			_isNegative = isNegative;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			IsFormed = true;
			return new DecimalIndicatorValue(this, (_isNegative ? -1 : 1) * Math.Abs(_line1.GetCurrentValue() - _line2.GetCurrentValue()));
		}

		/// <summary>
		/// Создать копию данного объекта.
		/// </summary>
		/// <returns>Копия данного объекта.</returns>
		public override IIndicator Clone()
		{
			return new GatorHistogram((AlligatorLine)_line1.Clone(), (AlligatorLine)_line2.Clone(), _isNegative) { Name = Name };
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			_line1.LoadNotNull(settings, "line1");
			_line2.LoadNotNull(settings, "line2");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("line1", _line1.Save());
			settings.SetValue("line2", _line2.Save());
		}
	}
}