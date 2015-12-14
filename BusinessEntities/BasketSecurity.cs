#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: BasketSecurity.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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