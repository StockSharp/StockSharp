#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: PortfolioLookupMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.ComponentModel;

	using Ecng.Common;

	/// <summary>
	/// Message portfolio lookup for specified criteria.
	/// </summary>
	[DataContract]
	[Serializable]
	public class PortfolioLookupMessage : PortfolioMessage, INullableSecurityIdMessage, IStrategyIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioLookupMessage"/>.
		/// </summary>
		public PortfolioLookupMessage()
			: base(MessageTypes.PortfolioLookup)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public string StrategyId { get; set; }

		/// <summary>
		/// Side.
		/// </summary>
		[DataMember]
		public Sides? Side { get; set; }

		/// <inheritdoc />
		public override DataType DataType => DataType.PositionChanges;

		/// <inheritdoc />
		[TypeConverter(typeof(StringToSecurityIdTypeConverter))]
		public SecurityId? SecurityId { get; set; }

		/// <summary>
		/// Create a copy of <see cref="PortfolioLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new PortfolioLookupMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		protected virtual void CopyTo(PortfolioLookupMessage destination)
		{
			base.CopyTo(destination);

			destination.SecurityId = SecurityId;
			destination.StrategyId = StrategyId;
			destination.Side = Side;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString();

			if (!IsSubscribe)
				str += $",IsSubscribe={IsSubscribe}";

			if (SecurityId != null)
				str += $",Sec={SecurityId}";

			if (!StrategyId.IsEmpty())
				str += $",Strategy={StrategyId}";

			if (Side != null)
				str += $",Side={Side.Value}";

			return str;
		}
	}
}