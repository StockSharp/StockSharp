#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: BasketSecurity.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

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
		public abstract IEnumerable<SecurityId> InnerSecurityIds { get; }

		/// <summary>
		/// Save security state to string.
		/// </summary>
		/// <returns>String.</returns>
		public abstract string ToSerializedString();

		/// <summary>
		/// Load security state from <paramref name="text"/>.
		/// </summary>
		/// <param name="text">Value, received from <see cref="ToSerializedString"/>.</param>
		public abstract void FromSerializedString(string text);
	}
}