namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;
	using Ecng.Serialization;

	/// <summary>
	/// Command types.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
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
	[System.Runtime.Serialization.DataContract]
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
	[System.Runtime.Serialization.DataContract]
	public class CommandMessage : BaseRequestMessage
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
			: base(type)
		{
		}

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
		/// Identifier.
		/// </summary>
		[DataMember]
		public string ObjectId { get; set; }

		[field: NonSerialized]
		private IDictionary<string, Tuple<string, string>> _parameters = new Dictionary<string, Tuple<string, string>>();

		/// <summary>
		/// Parameters.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		public IDictionary<string, Tuple<string, string>> Parameters => _parameters;

		/// <summary>
		/// Create a copy of <see cref="CommandMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new CommandMessage();

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		protected void CopyTo(CommandMessage destination)
		{
			base.CopyTo(destination);

			destination.Command = Command;
			destination.Scope = Scope;
			destination.ObjectId = ObjectId;
			destination.Parameters.AddRange(Parameters);
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.Command;

		/// <inheritdoc />
		public override string ToString()
			=> base.ToString() + $",Cmd={Command},Scp={Scope},Id={ObjectId}";
	}
}