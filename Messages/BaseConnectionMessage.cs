#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: BaseConnectionMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	/// <summary>
	/// Base connect/disconnect message.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class BaseConnectionMessage : Message, IErrorMessage
	{
		/// <summary>
		/// Initialize <see cref="BaseConnectionMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected BaseConnectionMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		[DataMember]
		[XmlIgnore]
		public Exception Error { get; set; }

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected virtual void CopyTo(BaseConnectionMessage destination)
		{
			base.CopyTo(destination);

			destination.Error = Error;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + (Error == null ? null : $",Error={Error.Message}");
		}
	}
}