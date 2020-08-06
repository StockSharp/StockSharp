#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: Message.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// <see cref="Message"/> offline modes.
	/// </summary>
	public enum MessageOfflineModes
	{
		/// <summary>
		/// None.
		/// </summary>
		None,

		/// <summary>
		/// Ignore offline mode and continue processing.
		/// </summary>
		Ignore,

		/// <summary>
		/// Cancel message processing and create reply.
		/// </summary>
		Cancel,
	}

	/// <summary>
	/// Message loopback modes.
	/// </summary>
	public enum MessageBackModes
	{
		/// <summary>
		/// None.
		/// </summary>
		None,

		/// <summary>
		/// Direct.
		/// </summary>
		Direct,

		/// <summary>
		/// Via whole adapters chain.
		/// </summary>
		Chain,
	}

	/// <summary>
	/// A message containing market data or command.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class Message : Cloneable<Message>, IExtendableEntity, IMessage
	{
		/// <inheritdoc />
		[DisplayNameLoc(LocalizedStrings.Str203Key)]
		[DescriptionLoc(LocalizedStrings.Str204Key)]
		[MainCategory]
		[DataMember]
		public DateTimeOffset LocalTime { get; set; }

		[field: NonSerialized]
		private readonly MessageTypes _type;

		/// <inheritdoc />
		public MessageTypes Type => _type;

		[field: NonSerialized]
		private IDictionary<string, object> _extensionInfo;

		/// <inheritdoc />
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<string, object> ExtensionInfo
		{
			get => _extensionInfo;
			set => _extensionInfo = value;
		}

		/// <summary>
		/// Is loopback message.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Obsolete("Use BackMode property.")]
		public bool IsBack
		{
			get => this.IsBack();
			set => BackMode = value ? MessageBackModes.Direct : MessageBackModes.None;
		}

		/// <inheritdoc />
		[Ignore]
		[XmlIgnore]
		public MessageBackModes BackMode { get; set; }

		/// <summary>
		/// Offline mode handling message.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		public MessageOfflineModes OfflineMode { get; set; }

		/// <inheritdoc />
		[Ignore]
		[XmlIgnore]
		public IMessageAdapter Adapter { get; set; }

		/// <summary>
		/// Initialize <see cref="Message"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected Message(MessageTypes type)
		{
			_type = type;
#if MSG_TRACE
			StackTrace = Environment.StackTrace;
#endif
		}

#if MSG_TRACE
		internal string StackTrace;
#endif

		/// <inheritdoc />
		public override string ToString()
		{
			var str = Type + $",T(L)={LocalTime:yyyy/MM/dd HH:mm:ss.fff}";

			if (BackMode != default)
				str += $",Back={BackMode}";

			if (OfflineMode != default)
				str += $",Offline={OfflineMode}";

			return str;
		}

		/// <summary>
		/// Create a copy of <see cref="Message"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public abstract override Message Clone();

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected void CopyTo(Message destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			destination.LocalTime = LocalTime;
#if MSG_TRACE
			destination.StackTrace = StackTrace;
#endif
			this.CopyExtensionInfo(destination);
		}
	}
}