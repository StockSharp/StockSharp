#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: ErrorMessage.cs
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
	/// Error message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ErrorMessage : Message, IErrorMessage, IOriginalTransactionIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessage"/>.
		/// </summary>
		public ErrorMessage()
			: base(MessageTypes.Error)
		{
		}

		/// <inheritdoc />
		[DataMember]
		[XmlIgnore]
		public Exception Error { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Error={Error?.Message},OrigTrId={OriginalTransactionId}";
		}

		/// <summary>
		/// Create a copy of <see cref="ErrorMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ErrorMessage
			{
				Error = Error,
				OriginalTransactionId = OriginalTransactionId,
			};

			CopyTo(clone);

			return clone;
		}
	}
}