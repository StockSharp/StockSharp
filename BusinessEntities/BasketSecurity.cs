namespace StockSharp.BusinessEntities
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	/// <summary>
	/// Корзина инструментов.
	/// </summary>
	public abstract class BasketSecurity : Security
	{
		/// <summary>
		/// Инициализировать <see cref="BasketSecurity"/>.
		/// </summary>
		protected BasketSecurity()
		{
		}

		/// <summary>
		/// Инструменты, из которых создана данная корзина.
		/// </summary>
		[Browsable(false)]
		public abstract IEnumerable<Security> InnerSecurities { get; }

		/// <summary>
		/// Проверить, используется ли указанный инструмент в настоящее время.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо проверить.</param>
		/// <returns><see langword="true"/>, если указанный инструмент используется в настоящее время, иначе, <see langword="false"/>.</returns>
		public virtual bool Contains(Security security)
		{
			return InnerSecurities.Any(innerSecurity =>
			{
				var basket = innerSecurity as BasketSecurity;

				if (basket == null)
					return innerSecurity == security;

				return basket.Contains(security);
			});
		}
	}
}