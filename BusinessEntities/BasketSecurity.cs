namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	/// <summary>
	/// Instruments basket.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class BasketSecurity : Security
	{
		/// <summary>
		/// Initialize <see cref="BasketSecurity"/>.
		/// </summary>
		protected BasketSecurity()
		{
		}

		/// <summary>
		/// Instruments, from which this basket is created.
		/// </summary>
		[Browsable(false)]
		public abstract IEnumerable<Security> InnerSecurities { get; }

		/// <summary>
		/// To check whether specified instrument is used now.
		/// </summary>
		/// <param name="security">The instrument that should be checked.</param>
		/// <returns><see langword="true" />, if specified instrument is used now, otherwise <see langword="false" />.</returns>
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