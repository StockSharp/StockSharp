namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// ПикБар.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/PeakBar.ashx
	/// </remarks>
	[DisplayName("PeakBar")]
	[DescriptionLoc(LocalizedStrings.Str817Key)]
	public class PeakBar : BaseIndicator<decimal>
	{
		private decimal _currentMaximum = decimal.MinValue;

		private int _currentBarCount;

		private int _valueBarCount;

		/// <summary>
		/// Создать <see cref="PeakBar"/>.
		/// </summary>
		public PeakBar()
			: base(typeof(Candle))
		{
		}

		private Unit _reversalAmount = new Unit();

		/// <summary>
		/// Порог изменения индикатора.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str783Key)]
		[DescriptionLoc(LocalizedStrings.Str784Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Unit ReversalAmount
		{
			get { return _reversalAmount; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_reversalAmount = value;

				Reset();
			}
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			try
			{
				if (candle.HighPrice > _currentMaximum)
				{
					_currentMaximum = candle.HighPrice;
					_valueBarCount = _currentBarCount;
				}
				else if (candle.LowPrice <= _currentMaximum - ReversalAmount)
				{
					IsFormed = true;
					return new DecimalIndicatorValue(this, _valueBarCount);
				}

				return new DecimalIndicatorValue(this, this.GetCurrentValue());
			}
			finally
			{
				if(input.IsFinal)
					_currentBarCount++;
			}
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			ReversalAmount.Type = settings.GetValue<UnitTypes>("ReversalAmountType");
			ReversalAmount.Value = settings.GetValue<decimal>("ReversalAmountValue");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("ReversalAmountType", ReversalAmount.Type);
			settings.SetValue("ReversalAmountValue", ReversalAmount.Value);
		}
	}
}