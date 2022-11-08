namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Collections;

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
			: base(MessageTypes.StrategyInfo)
		{
		}

		/// <summary>
		/// Strategy server ID.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Product ID.
		/// </summary>
		[DataMember]
		public long ProductId { get; set; }

		/// <summary>
		/// Strategy ID.
		/// </summary>
		[DataMember]
		public Guid StrategyId { get; set; }

		/// <summary>
		/// Strategy name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Strategy parameters.
		/// </summary>
		[DataMember]
		public IDictionary<string, (string type, string value)> Parameters { get; } = new Dictionary<string, (string type, string value)>();

		/// <summary>
		/// The creation date.
		/// </summary>
		[DataMember]
		public DateTimeOffset CreationDate { get; set; }

		/// <inheritdoc />
		public override DataType DataType => DataType.Info;

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Id={StrategyId},Name={Name},Params={Parameters.Select(p => $"{p.Key}={p.Value}").JoinComma()}";
		}

		/// <inheritdoc />
		public override void CopyTo(StrategyInfoMessage destination)
		{
			base.CopyTo(destination);

			destination.Id = Id;
			destination.ProductId = ProductId;
			destination.StrategyId = StrategyId;
			destination.Name = Name;
			destination.CreationDate = CreationDate;
			destination.Parameters.AddRange(Parameters);
		}
	}
}