namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Message strategies lookup.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyLookupMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyLookupMessage"/>.
		/// </summary>
		public StrategyLookupMessage()
			: base(ExtendedMessageTypes.StrategyLookup)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => StrategyDataType.Info;

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
			base.CopyTo(destination);

			return destination;
		}
	}
}