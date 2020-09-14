#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderGroupCancelMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The message containing the order group cancel filter.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderGroupCancelMessage : OrderMessage
	{
		/// <summary>
		/// <see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str226Key)]
		[DescriptionLoc(LocalizedStrings.Str227Key)]
		[MainCategory]
		public bool? IsStop { get; set; }

		/// <summary>
		/// Order side. If the value is <see langword="null" />, the direction does not use.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str228Key)]
		[MainCategory]
		public Sides? Side { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderGroupCancelMessage"/>.
		/// </summary>
		public OrderGroupCancelMessage()
			: base(MessageTypes.OrderGroupCancel)
		{
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",IsStop={IsStop},Side={Side}";
		}

		/// <summary>
		/// Create a copy of <see cref="OrderGroupCancelMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderGroupCancelMessage();

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(OrderGroupCancelMessage destination)
		{
			base.CopyTo(destination);

			destination.IsStop = IsStop;
			destination.Side = Side;
		}
	}
}