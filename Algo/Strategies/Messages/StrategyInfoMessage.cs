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
	public class StrategyInfoMessage : BaseSubscriptionIdMessage<StrategyInfoMessage>
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
		public IDictionary<string, Tuple<string, string>> Parameters { get; } = new Dictionary<string, Tuple<string, string>>();

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Id={StrategyId},Name={StrategyName},Params={Parameters.Select(p => $"{p.Key}={p.Value}").Join(",")}";
		}

		/// <inheritdoc />
		public override void CopyTo(StrategyInfoMessage destination)
		{
			base.CopyTo(destination);

			destination.StrategyName = StrategyName;
			destination.StrategyId = StrategyId;
			destination.Parameters.AddRange(Parameters);
		}
	}
}