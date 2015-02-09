namespace StockSharp.Algo.Derivatives
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Построитель синтетических позиций.
	/// </summary>
	public class Synthetic
	{
		private readonly Security _security;
		private readonly ISecurityProvider _provider;

		/// <summary>
		/// Создать <see cref="Synthetic"/>.
		/// </summary>
		/// <param name="security">Инструмент (опцион или базовый актив).</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		public Synthetic(Security security, ISecurityProvider provider)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (provider == null)
				throw new ArgumentNullException("provider");

			_security = security;
			_provider = provider;
		}

		private Security Option
		{
			get
			{
				_security.CheckOption();
				return _security;
			}
		}

		/// <summary>
		/// Получить синтетическую позицию для покупки опциона.
		/// </summary>
		/// <returns>Синтетическая позиция.</returns>
		public KeyValuePair<Security, Sides>[] Buy()
		{
			return Position(Sides.Buy);
		}

		/// <summary>
		/// Получить синтетическую позицию для продажи опциона.
		/// </summary>
		/// <returns>Синтетическая позиция.</returns>
		public KeyValuePair<Security, Sides>[] Sell()
		{
			return Position(Sides.Sell);
		}

		/// <summary>
		/// Получить синтетическую позицию для опциона.
		/// </summary>
		/// <param name="side">Направление основной позиции.</param>
		/// <returns>Синтетическая позиция.</returns>
		public KeyValuePair<Security, Sides>[] Position(Sides side)
		{
			var asset = Option.GetUnderlyingAsset(_provider);

			return new[]
			{
				new KeyValuePair<Security, Sides>(asset, Option.OptionType == OptionTypes.Call ? side : side.Invert()),
				new KeyValuePair<Security, Sides>(Option.GetOppositeOption(_provider), side)
			};
		}

		/// <summary>
		/// Получить опционную позицию для синтетической покупки базового актива.
		/// </summary>
		/// <param name="strike">Страйк.</param>
		/// <returns>Опционная позиция.</returns>
		public KeyValuePair<Security, Sides>[] Buy(decimal strike)
		{
			return Buy(strike, GetExpiryDate());
		}

		/// <summary>
		/// Получить опционную позицию для синтетической покупки базового актива.
		/// </summary>
		/// <param name="strike">Страйк.</param>
		/// <param name="expiryDate">Дата экспирации опциона.</param>
		/// <returns>Опционная позиция.</returns>
		public KeyValuePair<Security, Sides>[] Buy(decimal strike, DateTimeOffset expiryDate)
		{
			return Position(strike, expiryDate, Sides.Buy);
		}

		/// <summary>
		/// Получить опционную позицию для синтетической продажи базового актива.
		/// </summary>
		/// <param name="strike">Страйк.</param>
		/// <returns>Опционная позиция.</returns>
		public KeyValuePair<Security, Sides>[] Sell(decimal strike)
		{
			return Sell(strike, GetExpiryDate());
		}

		/// <summary>
		/// Получить опционную позицию для синтетической продажи базового актива.
		/// </summary>
		/// <param name="strike">Страйк.</param>
		/// <param name="expiryDate">Дата экспирации опциона.</param>
		/// <returns>Опционная позиция.</returns>
		public KeyValuePair<Security, Sides>[] Sell(decimal strike, DateTimeOffset expiryDate)
		{
			return Position(strike, expiryDate, Sides.Sell);
		}

		/// <summary>
		/// Получить опционную позицию для синтетического базового актива.
		/// </summary>
		/// <param name="strike">Страйк.</param>
		/// <param name="expiryDate">Дата экспирации опциона.</param>
		/// <param name="side">Направление основной позиции.</param>
		/// <returns>Опционная позиция.</returns>
		public KeyValuePair<Security, Sides>[] Position(decimal strike, DateTimeOffset expiryDate, Sides side)
		{
			var call = _security.GetCall(_provider, strike, expiryDate);
			var put = _security.GetPut(_provider, strike, expiryDate);

			return new[]
			{
				new KeyValuePair<Security, Sides>(call, side),
				new KeyValuePair<Security, Sides>(put, side.Invert())
			};
		}

		private DateTimeOffset GetExpiryDate()
		{
			if (_security.ExpiryDate == null)
				throw new InvalidOperationException(LocalizedStrings.Str712);

			return _security.ExpiryDate.Value;
		}
	}
}