namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	/// <summary>
	/// <para>Класс используется для идентификации индикатора в связке с источником, поставляющим данные для него.</para>
	/// <para>Класс реализует <see cref="IEquatable{T}"/>, равными считаются токены у которых одинаковый источник, при этом
	/// такой же индикатор, но не обязательно индикатор содержит те же значения.</para>
	/// </summary>
	public class IndicatorToken : Equatable<IndicatorToken>
	{
		/// <summary>
		/// Индикатор.
		/// </summary>
		public IIndicator Indicator { get; private set; }

		/// <summary>
		/// Источник данных для индикатора.
		/// </summary>
		public IIndicatorSource Source { get; private set; }

		/// <summary>
		/// Контейнер, хранящий данные индикаторов.
		/// </summary>
		public IIndicatorContainer Container { get; internal set; }

		/// <summary>
		/// Получить все значения индикатора.
		/// </summary>
		/// <returns>Все значения индикатора. Пустое множество, если значений нет.</returns>
		public IEnumerable<Tuple<IIndicatorValue, IIndicatorValue>> Values
		{
			get { return Container.GetValues(this); }
		}

		/// <summary>
		/// Создать <see cref="IndicatorToken"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="source">Источник данных для индикатора.</param>
		public IndicatorToken(IIndicator indicator, IIndicatorSource source)
		{
			if (indicator == null)
				throw new ArgumentNullException("indicator");

			if (source == null)
				throw new ArgumentNullException("source");

			Indicator = indicator;
			Source = source;
		}

		/// <summary>
		/// Получить значение индикатора по индексу.
		/// </summary>
		/// <param name="index">Порядковый номер значения с конца.</param>
		/// <returns>Найденное значение. Если значение не существует, то будет возвращено null.</returns>
		public virtual Tuple<IIndicatorValue, IIndicatorValue> GetValue(int index)
		{
			return Container.GetValue(this, index);
		}

		/// <summary>
		/// Возвращает хэш токена.
		/// </summary>
		/// <returns>Хэш токена.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return (Indicator.GetHashCode() * 397) ^ (Source.GetHashCode());
			}
		}

		/// <summary>
		/// Проверяет токен на равенство переданному в <paramref name="other"/>.
		/// </summary>
		/// <param name="other">Токен для сравнения.</param>
		/// <returns>true, если токены равны.</returns>
		protected override bool OnEquals(IndicatorToken other)
		{
			return Source.Equals(other.Source) && Indicator.Equals(other.Indicator);
		}

		/// <summary>
		/// Создать копию данного токена. Новый токен будет ссылаться на тот же самый индикатор и источник.
		/// </summary>
		/// <returns>Копия токена.</returns>
		public override IndicatorToken Clone()
		{
			return new IndicatorToken(Indicator, Source);
		}
	}
}