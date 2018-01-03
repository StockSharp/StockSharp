#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: OrderFail.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Description of the error that occurred during the registration or cancellation of the order.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public class OrderFail : IExtendableEntity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderFail"/>.
		/// </summary>
		public OrderFail()
		{
		}

		/// <summary>
		/// The order which was not registered or was canceled due to an error.
		/// </summary>
		[DataMember]
		[RelationSingle]
		public Order Order { get; set; }

		/// <summary>
		/// System information about error containing the reason for the refusal or cancel of registration.
		/// </summary>
		[DataMember]
		[BinaryFormatter]
		public Exception Error { get; set; }

		/// <summary>
		/// Server time.
		/// </summary>
		[DataMember]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Local time, when the error has been received.
		/// </summary>
		public DateTimeOffset LocalTime { get; set; }

		/// <summary>
		/// Extended information on the order with an error.
		/// </summary>
		[XmlIgnore]
		public IDictionary<string, object> ExtensionInfo
		{
			get => Order.ExtensionInfo;
			set => Order.ExtensionInfo = value;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return $"{Error?.Message}/{Order}";
		}
	}
}