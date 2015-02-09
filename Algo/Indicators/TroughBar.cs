namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// ВпадинаБар.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/TroughBar.ashx
	/// </remarks>
	[DisplayName("TroughBar")]
	[DescriptionLoc(LocalizedStrings.Str822Key)]
	public class TroughBar : BaseIndicator<decimal>
	{
		private decimal _currentMinimum = decimal.MaxValue;
		private int _currentBarCount;
		private int _valueBarCount;

		/// <summary>
		/// Создать <see cref="TroughBar"/>.
		/// </summary>
		public TroughBar()
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
				if (candle.LowPrice < _currentMinimum)
				{
					_currentMinimum = candle.LowPrice;
					_valueBarCount = _currentBarCount;
				}
				else if (candle.HighPrice >= _currentMinimum + ReversalAmount.Value)
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