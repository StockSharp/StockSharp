namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message contains information about strategy state or command to change state.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyStateMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyStateMessage"/>.
		/// </summary>
		public StrategyStateMessage()
			: base(ExtendedMessageTypes.StrategyState)
		{
		}

		/// <summary>
		/// Strategy ID.
		/// </summary>
		[DataMember]
		public Guid StrategyId { get; set; }

		/// <summary>
		/// Strategy type ID.
		/// </summary>
		[DataMember]
		public Guid StrategyTypeId { get; set; }

		/// <summary>
		/// Command.
		/// </summary>
		[DataMember]
		public string Command { get; set; }

		/// <summary>
		/// Error.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Transaction ID.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// ID of the original message <see cref="TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Statistics.
		/// </summary>
		[DataMember]
		public IDictionary<string, Tuple<string, string>> Statistics { get; private set; } = new Dictionary<string, Tuple<string, string>>();

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",TrId={TransactionId},Orig={OriginalTransactionId},Stat={Statistics.Select(p => $"{p.Key}={p.Value}").Join(",")}";

			if (!StrategyId.IsDefault())
				str += $",Id={StrategyId}";

			if (!StrategyTypeId.IsDefault())
				str += $",TypeId={StrategyTypeId}";

			if (!Command.IsEmpty())
				str += $",Command={Command}";

			if (Error != null)
				str += $",Error={Error.Message}";

			return str;
		}

		/// <summary>
		/// Create a copy of <see cref="StrategyStateMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new StrategyStateMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected StrategyStateMessage CopyTo(StrategyStateMessage destination)
		{
			destination.TransactionId = TransactionId;
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.StrategyId = StrategyId;
			destination.StrategyTypeId = StrategyTypeId;
			destination.Statistics = Statistics.ToDictionary();
			destination.Command = Command;
			destination.Error = Error;

			this.CopyExtensionInfo(destination);

			return destination;
		}
	}
}