#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: PortfolioLookupMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Message security lookup for specified criteria.
	/// </summary>
	[DataContract]
	[Serializable]
	public class PortfolioLookupMessage : PortfolioMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioLookupMessage"/>.
		/// </summary>
		public PortfolioLookupMessage()
			: base(MessageTypes.PortfolioLookup)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="PortfolioLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new PortfolioLookupMessage());
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + $",TransId={TransactionId},Curr={Currency},Board={BoardCode},IsSubscribe={IsSubscribe}";
		}
	}
}