namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;

	/// <summary>
	/// Command types.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum CommandTypes
	{
		/// <summary>
		/// Start.
		/// </summary>
		[EnumMember]
		Start,

		/// <summary>
		/// Stop.
		/// </summary>
		[EnumMember]
		Stop,

		/// <summary>
		/// Enable.
		/// </summary>
		[EnumMember]
		Enable,

		/// <summary>
		/// Disable.
		/// </summary>
		[EnumMember]
		Disable,

		/// <summary>
		/// Update settings.
		/// </summary>
		[EnumMember]
		Update,

		/// <summary>
		/// Add.
		/// </summary>
		[EnumMember]
		Add,

		/// <summary>
		/// Remove.
		/// </summary>
		[EnumMember]
		Remove,

		/// <summary>
		/// Request current state.
		/// </summary>
		[EnumMember]
		Get,

		/// <summary>
		/// Close position.
		/// </summary>
		[EnumMember]
		ClosePosition,

		/// <summary>
		/// Cancel orders.
		/// </summary>
		[EnumMember]
		CancelOrders,

		/// <summary>
		/// Register new order.
		/// </summary>
		[EnumMember]
		RegisterOrder,

		/// <summary>
		/// Cancel order.
		/// </summary>
		[EnumMember]
		CancelOrder,

		/// <summary>
		/// Restart.
		/// </summary>
		[EnumMember]
		Restart,

		/// <summary>
		/// Share.
		/// </summary>
		[EnumMember]
		Share,

		/// <summary>
		/// Unshare.
		/// </summary>
		[EnumMember]
		UnShare,

		/// <summary>
		/// List objects.
		/// </summary>
		[EnumMember]
		List,
	}

	/// <summary>
	/// Command scopes.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum CommandScopes
	{
		/// <summary>
		/// Application.
		/// </summary>
		[EnumMember]
		Application,

		/// <summary>
		/// Adapter.
		/// </summary>
		[EnumMember]
		Adapter,

		/// <summary>
		/// Strategy.
		/// </summary>
		[EnumMember]
		Strategy,

		/// <summary>
		/// Position.
		/// </summary>
		[EnumMember]
		Position,

		/// <summary>
		/// Order.
		/// </summary>
		[EnumMember]
		Order,

		/// <summary>
		/// File.
		/// </summary>
		[EnumMember]
		File,

		/// <summary>
		/// File group.
		/// </summary>
		[EnumMember]
		FileGroup,
	}

	/// <summary>
	/// The message contains information about command to change state.
	/// </summary>
	[Serializable]
	[DataContract]
	public class CommandMessage : Message, ITransactionIdMessage
	{
		/// <summary>
		/// Initialize <see cref="CommandMessage"/>.
		/// </summary>
		public CommandMessage()
			: this(MessageTypes.Command)
		{
		}

		/// <summary>
		/// Initialize <see cref="CommandMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected CommandMessage(MessageTypes type)
			: base(MessageTypes.Command)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Command.
		/// </summary>
		[DataMember]
		public CommandTypes Command { get; set; }

		/// <summary>
		/// Scope.
		/// </summary>
		[DataMember]
		public CommandScopes Scope { get; set; }

		/// <summary>
		/// Adapter identifier.
		/// </summary>
		[DataMember]
		public Guid ObjectId { get; set; }

		/// <summary>
		/// Parameters.
		/// </summary>
		[DataMember]
		[XmlIgnore]
		public IDictionary<string, Tuple<string, string>> Parameters { get; private set; } = new Dictionary<string, Tuple<string, string>>();

		/// <summary>
		/// Create a copy of <see cref="CommandMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new CommandMessage
			{
				TransactionId = TransactionId,
				Command = Command,
				Scope = Scope,
				ObjectId = ObjectId,
				Parameters = Parameters.ToDictionary(),
			};

			CopyTo(clone);

			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $",TrId={TransactionId},Cmd={Command},Scp={Scope},Id={ObjectId}";
	}
}