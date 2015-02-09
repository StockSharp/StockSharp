namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Источник данных для индикаторов, которые строяться на основе объектов <see cref="MarketDepth"/>.
	/// </summary>
	public class MarketDepthIndicatorSource : BaseIndicatorSource
	{
		private readonly MarketDepth _depth;
		private readonly Func<MarketDepth, decimal> _getPart;

		/// <summary>
		/// Создать <see cref="MarketDepthIndicatorSource"/>.
		/// </summary>
		/// <param name="depth">Стакан.</param>
		public MarketDepthIndicatorSource(MarketDepth depth)
			: this(depth, MarketDepthIndicatorValue.ByMiddle)
		{
		}

		/// <summary>
		/// Создать <see cref="MarketDepthIndicatorSource"/>.
		/// </summary>
		/// <param name="depth">Стакан.</param>
		/// <param name="getPart">Конвертер стакана, через который можно получить его параметр (например, цену лучшего бида <see cref="MarketDepth.BestBid"/>, середину спреда и т.д.).</param>
		public MarketDepthIndicatorSource(MarketDepth depth, Func<MarketDepth, decimal> getPart)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			if (getPart == null)
				throw new ArgumentNullException("getPart");

			_depth = depth;
			_getPart = getPart;

			_depth.DepthChanged += OnDepthChanged;
		}

		private void OnDepthChanged()
		{
			RaiseNewValue(new MarketDepthIndicatorValue(_depth.Clone(), _getPart));
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_depth.DepthChanged -= OnDepthChanged;
			base.DisposeManaged();
		}

		/// <summary>
		/// Проверяет равен ли текущий источник переданному.
		/// </summary>
		/// <returns>true, если текущий источник равен <paramref name="other"/> в противном случае false.</returns>
		/// <param name="other">Источник для сравнения.</param>
		public override bool Equals(IIndicatorSource other)
		{
			return base.Equals(other) &&
			       _depth.Equals(((MarketDepthIndicatorSource)other)._depth) &&
				   _getPart == ((MarketDepthIndicatorSource)other)._getPart;
		}

		/// <summary>
		/// Получить хэш код источника.
		/// </summary>
		/// <returns>Хэш код.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int result = base.GetHashCode();
				result = (result * 397) ^ _depth.GetHashCode();
				result = (result * 397) ^ (_getPart != null ? _getPart.GetHashCode() : 0);
				return result;
			}
		}
	}
}