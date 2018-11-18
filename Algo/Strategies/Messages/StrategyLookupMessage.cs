namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Message strategies lookup.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyLookupMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyLookupMessage"/>.
		/// </summary>
		public StrategyLookupMessage()
			: base(ExtendedMessageTypes.StrategyLookup)
		{
		}

		/// <summary>
		/// Transaction ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		public long TransactionId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="StrategyLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new StrategyLookupMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected StrategyLookupMessage CopyTo(StrategyLookupMessage destination)
		{
			destination.TransactionId = TransactionId;

			this.CopyExtensionInfo(destination);

			return destination;
		}
	}
}