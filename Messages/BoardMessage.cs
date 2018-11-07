#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: BoardMessage.cs
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
	/// The message contains information about the electronic board.
	/// </summary>
	[DataContract]
	[Serializable]
	public class BoardMessage : Message
	{
		/// <summary>
		/// Exchange code, which owns the board. Maybe be the same <see cref="Code"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExchangeInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str56Key)]
		[MainCategory]
		public string ExchangeCode { get; set; }

		/// <summary>
		/// Board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CodeKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[MainCategory]
		public string Code { get; set; }

		/// <summary>
		/// ID of the original message <see cref="BoardLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		///// <summary>
		///// Gets a value indicating whether the re-registration orders via <see cref="OrderReplaceMessage"/> as a single transaction.
		///// </summary>
		//[DataMember]
		//[DisplayNameLoc(LocalizedStrings.ReregisteringKey)]
		//[DescriptionLoc(LocalizedStrings.Str60Key)]
		//[MainCategory]
		//public bool IsSupportAtomicReRegister { get; set; }

		///// <summary>
		///// Are market type orders <see cref="OrderTypes.Market"/> supported.
		///// </summary>
		//[DataMember]
		//[DisplayNameLoc(LocalizedStrings.MarketOrdersKey)]
		//[DescriptionLoc(LocalizedStrings.MarketOrdersSupportedKey)]
		//[MainCategory]
		//public bool IsSupportMarketOrders { get; set; }

		/// <summary>
		/// Securities expiration times.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.Str64Key)]
		[MainCategory]
		public TimeSpan ExpiryTime { get; set; }

		private WorkingTime _workingTime = new WorkingTime();

		/// <summary>
		/// Board working hours.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.WorkingTimeKey)]
		[DescriptionLoc(LocalizedStrings.WorkingHoursKey)]
		[MainCategory]
		public WorkingTime WorkingTime
		{
			get => _workingTime;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (WorkingTime == value)
					return;

				_workingTime = value;
			}
		}

		[field: NonSerialized]
		private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

		/// <summary>
		/// Information about the time zone where the exchange is located.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TimeZoneKey)]
		[DescriptionLoc(LocalizedStrings.Str68Key)]
		[MainCategory]
		public TimeZoneInfo TimeZone
		{
			get => _timeZone;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (TimeZone == value)
					return;

				_timeZone = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoardMessage"/>.
		/// </summary>
		public BoardMessage()
			: base(MessageTypes.Board)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="BoardMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new BoardMessage
			{
				Code = Code,
				ExchangeCode = ExchangeCode,
				ExpiryTime = ExpiryTime,
				//IsSupportAtomicReRegister = IsSupportAtomicReRegister,
				//IsSupportMarketOrders = IsSupportMarketOrders,
				WorkingTime = WorkingTime.Clone(),
				TimeZone = TimeZone,
				OriginalTransactionId = OriginalTransactionId,
			};
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",Code={Code},Ex={ExchangeCode}";
		}
	}
}