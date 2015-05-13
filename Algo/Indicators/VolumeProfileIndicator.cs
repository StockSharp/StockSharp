namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Профайл объема.
	/// </summary>
	[DisplayName("VolumeProfile")]
	[DescriptionLoc(LocalizedStrings.Str729Key)]
	public class VolumeProfileIndicator : BaseIndicator
	{
		private readonly Dictionary<decimal, decimal> _levels = new Dictionary<decimal, decimal>();

		/// <summary>
		/// Создать <see cref="StockSharp.Algo.Indicators.VolumeProfileIndicator"/>.
		/// </summary>
		public VolumeProfileIndicator()
		{
			Step = 1;
		}

		/// <summary>
		/// Шаг группировки.
		/// </summary>
		public decimal Step { get; set; }

		/// <summary>
		/// Использовать в расчетах суммарный объем (когда свечи не содержат VolumeProfile).
		/// </summary>
		public bool UseTotalVolume { get; set; }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var result = new VolumeProfileIndicatorValue(this);

			if (!input.IsFinal)
				return result;

			IsFormed = true;

			var candle = input.GetValue<Candle>();

			if (!UseTotalVolume)
			{
				foreach (var priceLevel in candle.VolumeProfileInfo.PriceLevels)
					AddVolume(priceLevel.Price, priceLevel.BuyVolume + priceLevel.SellVolume);
			}
			else
				AddVolume(candle.ClosePrice, candle.TotalVolume);

			foreach (var level in _levels)
			{
				result.Levels.Add(level.Key, level.Value);
			}

			return result;
		}

		private void AddVolume(decimal price, decimal volume)
		{
			var level = (int)(price / Step) * Step;
			var currentValue = _levels.TryGetValue(level);

			_levels[level] = currentValue + volume;
		}
	}

	/// <summary>
	/// Значение индикатора <see cref="VolumeProfileIndicator"/>, которое получается в результате вычисления.
	/// </summary>
	public class VolumeProfileIndicatorValue : SingleIndicatorValue<IDictionary<decimal, decimal>>
	{
		/// <summary>
		/// Создать <see cref="VolumeProfileIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		public VolumeProfileIndicatorValue(IIndicator indicator)
			: base(indicator)
		{
			Levels = new Dictionary<decimal, decimal>();
		}

		/// <summary>
		/// Вложенные значения.
		/// </summary>
		public IDictionary<decimal, decimal> Levels { get; private set; }

		/// <summary>
		/// Поддерживает ли значение необходимый для индикатора тип данных.
		/// </summary>
		/// <param name="valueType">Тип данных, которым оперирует индикатор.</param>
		/// <returns><see langword="true"/>, если тип данных поддерживается, иначе, <see langword="false"/>.</returns>
		public override bool IsSupport(Type valueType)
		{
			return valueType == typeof(decimal);
		}

		/// <summary>
		/// Получить значение по типу данных.
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <returns>Значение.</returns>
		public override T GetValue<T>()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Изменить входное значение индикатора новым значением (например, оно получено от другого индикатора).
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <returns>Измененная копия входного значения.</returns>
		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Сравнить с другим значением индикатора.
		/// </summary>
		/// <param name="other">Другое значение, с которым необходимо сравнивать.</param>
		/// <returns>Код сравнения.</returns>
		public override int CompareTo(IIndicatorValue other)
		{
			throw new NotSupportedException();
		}
	}
}
