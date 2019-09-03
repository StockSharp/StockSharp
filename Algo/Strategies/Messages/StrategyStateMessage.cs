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
	/// Strategy commands.
	/// </summary>
	public enum StrategyCommands
	{
		/// <summary>
		/// Request current state.
		/// </summary>
		RequestState,

		/// <summary>
		/// Cancel orders.
		/// </summary>
		CancelOrders,

		/// <summary>
		/// Register new order.
		/// </summary>
		RegisterOrder,

		/// <summary>
		/// Cancel order.
		/// </summary>
		CancelOrder,
		
		/// <summary>
		/// Close position.
		/// </summary>
		ClosePosition,

		/// <summary>
		/// Start.
		/// </summary>
		Start,

		/// <summary>
		/// Stop.
		/// </summary>
		Stop,
	}

	/// <summary>
	/// The message contains information about strategy state or command to change state.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategyStateMessage : BaseResultMessage<StrategyStateMessage>, ITransactionIdMessage
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
		public string StrategyTypeId { get; set; }

		/// <summary>
		/// Command.
		/// </summary>
		[DataMember]
		public StrategyCommands? Command { get; set; }

		/// <summary>
		/// Transaction ID.
		/// </summary>
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
			var str = base.ToString() + $",TrId={TransactionId},Orig={OriginalTransactionId},Stat={Statistics.Select(p => $"{p.Key}={p.Value}").Join(",")}";

			if (!StrategyId.IsDefault())
				str += $",Id={StrategyId}";

			if (!StrategyTypeId.IsEmpty())
				str += $",TypeId={StrategyTypeId}";

			if (Command != null)
				str += $",Command={Command}";

			if (Error != null)
				str += $",Error={Error.Message}";

			return str;
		}

		/// <inheritdoc />
		protected override void CopyTo(StrategyStateMessage destination)
		{
			base.CopyTo(destination);

			destination.TransactionId = TransactionId;
			destination.StrategyId = StrategyId;
			destination.StrategyTypeId = StrategyTypeId;
			destination.Statistics = Statistics.ToDictionary();
			destination.Command = Command;
		}
	}
}