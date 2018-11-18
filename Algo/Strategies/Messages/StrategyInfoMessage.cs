namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The message contains information about strategy.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyInfoMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyInfoMessage"/>.
		/// </summary>
		public StrategyInfoMessage()
			: base(ExtendedMessageTypes.StrategyInfo)
		{
		}

		/// <summary>
		/// Strategy ID.
		/// </summary>
		[DataMember]
		public Guid StrategyId { get; set; }

		/// <summary>
		/// Strategy name.
		/// </summary>
		[DataMember]
		public string StrategyName { get; set; }

		/// <summary>
		/// Strategy parameters.
		/// </summary>
		[DataMember]
		public IDictionary<string, Tuple<string, string>> Parameters { get; private set; } = new Dictionary<string, Tuple<string, string>>();

		/// <summary>
		/// ID of the original message <see cref="StrategyLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Id={StrategyId},Name={StrategyName},Params={Parameters.Select(p => $"{p.Key}={p.Value}").Join(",")}";
		}

		/// <summary>
		/// Create a copy of <see cref="StrategyInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new StrategyInfoMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected StrategyInfoMessage CopyTo(StrategyInfoMessage destination)
		{
			destination.StrategyName = StrategyName;
			destination.StrategyId = StrategyId;
			destination.Parameters = Parameters.ToDictionary();
			destination.OriginalTransactionId = OriginalTransactionId;

			this.CopyExtensionInfo(destination);

			return destination;
		}
	}
}