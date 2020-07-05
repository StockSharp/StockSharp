#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: BaseChangeMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// A message containing changes.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	/// <typeparam name="TField">Changes type.</typeparam>
	[DataContract]
	[Serializable]
	public abstract class BaseChangeMessage<TMessage, TField> :	BaseSubscriptionIdMessage<TMessage>,
		IServerTimeMessage, IGeneratedMessage
		where TMessage : BaseChangeMessage<TMessage, TField>, new()
	{
		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.Str168Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <inheritdoc />
		[DataMember]
		public DataType BuildFrom { get; set; }

		/// <summary>
		/// Changes.
		/// </summary>
		[Browsable(false)]
		//[DataMember]
		[XmlIgnore]
		public IDictionary<TField, object> Changes { get; } = new Dictionary<TField, object>();

		/// <summary>
		/// Initialize <see cref="BaseChangeMessage{TMessage,TField}"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected BaseChangeMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <inheritdoc />
		public override void CopyTo(TMessage destination)
		{
			base.CopyTo(destination);

			destination.ServerTime = ServerTime;
			destination.BuildFrom = BuildFrom;

			destination.Changes.AddRange(Changes);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",T(S)={ServerTime:yyyy/MM/dd HH:mm:ss.fff}";
		}
	}
}