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
	public class StrategyStateMessage : BaseSubscriptionIdMessage<StrategyStateMessage>, ITransactionIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyStateMessage"/>.
		/// </summary>
		public StrategyStateMessage()
			: base(ExtendedMessageTypes.StrategyState)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => StrategyDataType.State;

		/// <summary>
		/// Strategy ID.
		/// </summary>
		[DataMember]
		public Guid StrategyId { get; set; }

		/// <summary>
		/// Strategy type ID.
		/// </summary>
		[DataMember]
		public string StrategyTypeId { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Statistics.
		/// </summary>
		[DataMember]
		public IDictionary<string, Tuple<string, string>> Statistics { get; private set; } = new Dictionary<string, Tuple<string, string>>();

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",TrId={TransactionId},Orig={OriginalTransactionId},Stat={Statistics.Select(p => $"{p.Key}={p.Value}").JoinComma()}";

			if (!StrategyId.IsDefault())
				str += $",Id={StrategyId}";

			if (!StrategyTypeId.IsEmpty())
				str += $",TypeId={StrategyTypeId}";

			return str;
		}

		/// <inheritdoc />
		public override void CopyTo(StrategyStateMessage destination)
		{
			base.CopyTo(destination);

			destination.TransactionId = TransactionId;
			destination.StrategyId = StrategyId;
			destination.StrategyTypeId = StrategyTypeId;
			destination.Statistics = Statistics.ToDictionary();
		}
	}
}