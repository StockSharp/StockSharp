namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

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
			return base.ToString() + ",TransId={0},Curr={1},Board={2},IsSubscribe={3}".Put(TransactionId, Currency, BoardCode, IsSubscribe);
		}
	}
}