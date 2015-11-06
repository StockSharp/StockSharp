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
	/// A message containing market data or command.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class Message : Cloneable<Message>, IExtendableEntity
	{
		/// <summary>
		/// Local time label when a message was received/created.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str203Key)]
		[DescriptionLoc(LocalizedStrings.Str204Key)]
		[MainCategory]
		[DataMember]
		public DateTime LocalTime { get; set; }

		[field: NonSerialized]
		private readonly MessageTypes _type;

		/// <summary>
		/// Message type.
		/// </summary>
		public MessageTypes Type => _type;

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended information.
		/// </summary>
		/// <remarks>
		/// Necessary to keep additional information associated with the message.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set { _extensionInfo = value; }
		}

		/// <summary>
		/// Is loopback message.
		/// </summary>
		public bool IsBack { get; set; }

		/// <summary>
		/// Source adapter. Can be <see langword="null" />.
		/// </summary>
		public IMessageAdapter Adapter { get; set; }

		/// <summary>
		/// Initialize <see cref="Message"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected Message(MessageTypes type)
		{
			_type = type;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Type + ",T(L)={0:yyyy/MM/dd HH:mm:ss.fff}".Put(LocalTime);
		}

		/// <summary>
		/// Create a copy of <see cref="Message"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			throw new NotSupportedException();
		}
	}
}