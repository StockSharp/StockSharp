namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The message contains information about the electronic board.
	/// </summary>
	[DataContract]
	[Serializable]
	public class BoardMessage : Message
	{
		/// <summary>
		/// Exchange code, which owns the board. Maybe be the same <see cref="BoardMessage.Code"/>.
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
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string Code { get; set; }

		/// <summary>
		/// Gets a value indicating whether the re-registration orders via <see cref="OrderReplaceMessage"/> as a single transaction.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ReregisteringKey)]
		[DescriptionLoc(LocalizedStrings.Str60Key)]
		[MainCategory]
		public bool IsSupportAtomicReRegister { get; set; }

		/// <summary>
		/// Are market type orders <see cref="OrderTypes.Market"/> supported.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarketOrdersKey)]
		[DescriptionLoc(LocalizedStrings.MarketOrdersSupportedKey)]
		[MainCategory]
		public bool IsSupportMarketOrders { get; set; }

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
			get { return _workingTime; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

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
			get { return _timeZone; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

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
				IsSupportAtomicReRegister = IsSupportAtomicReRegister,
				IsSupportMarketOrders = IsSupportMarketOrders,
				WorkingTime = WorkingTime.Clone(),
				TimeZone = TimeZone,
			};
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Code={0},Ex={1}".Put(Code, ExchangeCode);
		}
	}
}