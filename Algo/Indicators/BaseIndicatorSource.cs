namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Базовый источник данных для индикаторов.
	/// </summary>
	public abstract class BaseIndicatorSource : Equatable<IIndicatorSource>, IIndicatorSource
	{
		/// <summary>
		/// Инициализировать <see cref="BaseIndicatorSource"/>.
		/// </summary>
		protected BaseIndicatorSource()
		{
		}

		/// <summary>
		/// Событие появления новых данных.
		/// </summary>
		public event Action<IIndicatorValue> NewValue;

		/// <summary>
		/// Вызвать событие <see cref="NewValue"/>.
		/// </summary>
		/// <param name="value">Новые данные.</param>
		protected virtual void RaiseNewValue(IIndicatorValue value)
		{
			NewValue.SafeInvoke(value);
		}

		/// <summary>
		/// Получить хэш код источника.
		/// </summary>
		/// <returns>Хэш код.</returns>
		public override int GetHashCode()
		{
			return 0;
		}
	}
}