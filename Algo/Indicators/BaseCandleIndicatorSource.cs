namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;

	using StockSharp.Algo.Candles;

	/// <summary>
	/// Базовый источник данных для индикаторов, которые строяться на основе объектов <see cref="Candle"/>.
	/// </summary>
	public abstract class BaseCandleIndicatorSource : BaseIndicatorSource
	{
		private readonly Func<Candle, decimal> _getPart;

		/// <summary>
		/// Инициализировать <see cref="BaseCandleIndicatorSource"/>.
		/// </summary>
		protected BaseCandleIndicatorSource()
		{
		}

		/// <summary>
		/// Инициализировать <see cref="BaseCandleIndicatorSource"/>.
		/// </summary>
		/// <param name="getPart">Конвертер свечки, через которую можно получить ее параметр (цену закрытия <see cref="Candle.ClosePrice"/>, цену открытия <see cref="Candle.OpenPrice"/> и т.д.).</param>
		protected BaseCandleIndicatorSource(Func<Candle, decimal> getPart)
		{
			if (getPart == null)
				throw new ArgumentNullException("getPart");

			_getPart = getPart;
		}

		/// <summary>
		/// Передать новую свечку.
		/// </summary>
		/// <param name="candle">Новая свечка.</param>
		public virtual void NewCandle(Candle candle)
		{
			RaiseNewValue(new CandleIndicatorValue(candle, _getPart));
		}

		/// <summary>
		/// Проверяет равен ли текущий источник переданному.
		/// </summary>
		/// <returns>true, если текущий источник равен <paramref name="other"/> в противном случае false.</returns>
		/// <param name="other">Источник для сравнения.</param>
		public override bool Equals(IIndicatorSource other)
		{
			return base.Equals(other) && _getPart == ((BaseCandleIndicatorSource)other)._getPart;
		}

		/// <summary>
		/// Получить хэш код источника.
		/// </summary>
		/// <returns>Хэш код.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ (_getPart != null ? _getPart.GetHashCode() : 0);
			}
		}
	}

	/// <summary>
	/// Источник данных для индикаторов, которые строяться на основе объектов <see cref="Candle"/>, поступающих из <see cref="ICandleManager"/>.
	/// </summary>
	public class CandleManagerIndicatorSource : BaseCandleIndicatorSource
	{
		private readonly ICandleManager _candleManager;

		/// <summary>
		/// Создать <see cref="CandleManagerIndicatorSource"/>.
		/// </summary>
		/// <param name="candleManager">Менеджер свечек.</param>
		public CandleManagerIndicatorSource(ICandleManager candleManager)
		{
			if (candleManager == null)
				throw new ArgumentNullException("candleManager");

			_candleManager = candleManager;
			_candleManager.Processing += OnProcessing;
		}

		/// <summary>
		/// Создать <see cref="CandleManagerIndicatorSource"/>.
		/// </summary>
		/// <param name="candleManager">Менеджер свечек.</param>
		/// <param name="getPart">Конвертер свечки, через которую можно получить ее параметр (цену закрытия <see cref="Candle.ClosePrice"/>, цену открытия <see cref="Candle.OpenPrice"/> и т.д.).</param>
		public CandleManagerIndicatorSource(ICandleManager candleManager, Func<Candle, decimal> getPart)
			: base(getPart)
		{
			if (candleManager == null)
				throw new ArgumentNullException("candleManager");

			_candleManager = candleManager;
			_candleManager.Processing += OnProcessing;
		}

		private void OnProcessing(CandleSeries series, Candle candle)
		{
			if (candle.State == CandleStates.Finished)
				NewCandle(candle);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_candleManager.Processing -= OnProcessing;
			base.DisposeManaged();
		}

		/// <summary>
		/// Проверяет равен ли текущий источник переданному.
		/// </summary>
		/// <returns>true, если текущий источник равен <paramref name="other"/> в противном случае false.</returns>
		/// <param name="other">Источник для сравнения.</param>
		public override bool Equals(IIndicatorSource other)
		{
			return base.Equals(other) && _candleManager.Equals(((CandleManagerIndicatorSource)other)._candleManager);
		}

		/// <summary>
		/// Получить хэш код источника.
		/// </summary>
		/// <returns>Хэш код.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ _candleManager.GetHashCode();
			}
		}
	}

	/// <summary>
	/// <para>Источник данных для индикаторов, которые строяться на основе объектов <see cref="Candle"/>.</para>
	/// <para>В отличии от остальных источников, для данного проверка равенства осуществляется через ReferenceEquals,
	/// поэтому каждая регистрация индикатора с новым источником методом <see cref="IndicatorManager.RegisterIndicator"/>
	/// будит приводить не к поиску существующего источника в коллекции менеджера, а к регистрации нового источника.</para>
	/// </summary>
	public class RawCandleIndicatorSource : BaseCandleIndicatorSource
	{
		/// <summary>
		/// Создать <see cref="RawCandleIndicatorSource"/>.
		/// </summary>
		public RawCandleIndicatorSource()
		{
		}

		/// <summary>
		/// Создать <see cref="RawCandleIndicatorSource"/>.
		/// </summary>
		/// <param name="getPart">Конвертер свечки, через которую можно получить ее параметр (цену закрытия <see cref="Candle.ClosePrice"/>, цену открытия <see cref="Candle.OpenPrice"/> и т.д.).</param>
		public RawCandleIndicatorSource(Func<Candle, decimal> getPart)
			: base(getPart)
		{
			_currentObjectIndex = (_objectIndexer++);
		}

		/// <summary>
		/// Проверяет равен ли текущий источник переданному.
		/// </summary>
		/// <returns>true, если текущий источник равен <paramref name="other"/> в противном случае false.</returns>
		/// <param name="other">Источник для сравнения.</param>
		public override bool Equals(IIndicatorSource other)
		{
			return ReferenceEquals(this, other);
		}

		/// <summary>
		/// Получить хэш код источника.
		/// </summary>
		/// <returns>Хэш код.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return _currentObjectIndex;
			}
		}

		private static int _objectIndexer;
		private readonly int _currentObjectIndex;
	}
}