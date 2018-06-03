#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: ExchangeBoard.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Xml;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Reflection;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Information about electronic board.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public partial class ExchangeBoard : Equatable<ExchangeBoard>, IExtendableEntity, IPersistable, INotifyPropertyChanged
	{
		private const BindingFlags _publicStatic = BindingFlags.Public | BindingFlags.Static;

		/// <summary>
		/// To get a list of exchanges.
		/// </summary>
		/// <returns>Exchanges.</returns>
		public static IEnumerable<Exchange> EnumerateExchanges()
		{
			return typeof(Exchange).GetMembers<PropertyInfo>(_publicStatic, typeof(Exchange))
				.Select(prop => (Exchange)prop.GetValue(null, null));
		}

		/// <summary>
		/// To get a list of boards.
		/// </summary>
		/// <returns>Boards.</returns>
		public static IEnumerable<ExchangeBoard> EnumerateExchangeBoards()
		{
			return typeof(ExchangeBoard).GetMembers<PropertyInfo>(_publicStatic, typeof(ExchangeBoard))
				.Select(prop => (ExchangeBoard)prop.GetValue(null, null));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExchangeBoard"/>.
		/// </summary>
		public ExchangeBoard()
		{
			ExtensionInfo = new Dictionary<string, object>();
		}

		private string _code = string.Empty;

		/// <summary>
		/// Board code.
		/// </summary>
		[DataMember]
		[Identity]
		[DisplayNameLoc(LocalizedStrings.CodeKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[MainCategory]
		public string Code
		{
			get => _code;
			set
			{
				if (Code == value)
					return;

				_code = value ?? throw new ArgumentNullException(nameof(value));
				Notify(nameof(Code));
			}
		}

		private TimeSpan _expiryTime;

		/// <summary>
		/// Securities expiration times.
		/// </summary>
		[TimeSpan]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.Str64Key)]
		[MainCategory]
		[XmlIgnore]
		public TimeSpan ExpiryTime
		{
			get => _expiryTime;
			set
			{
				if (ExpiryTime == value)
					return;

				_expiryTime = value;
				Notify(nameof(ExpiryTime));
			}
		}

		/// <summary>
		/// Reserved.
		/// </summary>
		[Browsable(false)]
		[XmlElement(DataType = "duration", ElementName = nameof(ExpiryTime))]
		[Ignore]
		public string ExpiryTimeStr
		{
			// XmlSerializer does not support TimeSpan, so use this property for 
			// serialization instead.
			get => XmlConvert.ToString(ExpiryTime);
			set => ExpiryTime = value.IsEmpty() ? TimeSpan.Zero : XmlConvert.ToTimeSpan(value);
		}

		/// <summary>
		/// Exchange, where board is situated.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExchangeInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str479Key)]
		[MainCategory]
		public Exchange Exchange { get; set; }

		//private bool _isSupportAtomicReRegister;

		///// <summary>
		///// Gets a value indicating whether the re-registration orders via <see cref="OrderReplaceMessage"/> as a single transaction.
		///// </summary>
		//[DataMember]
		//[DisplayNameLoc(LocalizedStrings.ReregisteringKey)]
		//[DescriptionLoc(LocalizedStrings.Str60Key)]
		//[MainCategory]
		//public bool IsSupportAtomicReRegister
		//{
		//	get { return _isSupportAtomicReRegister; }
		//	set
		//	{
		//		_isSupportAtomicReRegister = value;
		//		Notify(nameof(IsSupportAtomicReRegister));
		//	}
		//}

		//private bool _isSupportMarketOrders;

		///// <summary>
		///// Are market type orders <see cref="OrderTypes.Market"/> supported.
		///// </summary>
		//[DataMember]
		//[DisplayNameLoc(LocalizedStrings.MarketOrdersKey)]
		//[DescriptionLoc(LocalizedStrings.MarketOrdersSupportedKey)]
		//[MainCategory]
		//public bool IsSupportMarketOrders
		//{
		//	get { return _isSupportMarketOrders; }
		//	set
		//	{
		//		_isSupportMarketOrders = value;
		//		Notify(nameof(IsSupportMarketOrders));
		//	}
		//}

		private WorkingTime _workingTime = new WorkingTime();

		/// <summary>
		/// Board working hours.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.WorkingTimeKey)]
		[DescriptionLoc(LocalizedStrings.WorkingHoursKey)]
		[MainCategory]
		[InnerSchema]
		public WorkingTime WorkingTime
		{
			get => _workingTime;
			set
			{
				if (WorkingTime == value)
					return;

				_workingTime = value ?? throw new ArgumentNullException(nameof(value));
				Notify(nameof(WorkingTime));
			}
		}

		[field: NonSerialized]
		private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

		/// <summary>
		/// Information about the time zone where the exchange is located.
		/// </summary>
		[TimeZoneInfo]
		[XmlIgnore]
		//[DataMember]
		public TimeZoneInfo TimeZone
		{
			get => _timeZone;
			set
			{
				if (TimeZone == value)
					return;

				_timeZone = value ?? throw new ArgumentNullException(nameof(value));
				Notify(nameof(TimeZone));
			}
		}

		/// <summary>
		/// Reserved.
		/// </summary>
		[Browsable(false)]
		[DataMember]
		[Ignore]
		public string TimeZoneStr
		{
			get => TimeZone.To<string>();
			set => TimeZone = value.To<TimeZoneInfo>();
		}

		[field: NonSerialized]
		private IDictionary<string, object> _extensionInfo;

		/// <summary>
		/// Extended exchange info.
		/// </summary>
		/// <remarks>
		/// Required if additional information associated with the exchange is stored in the program.
		/// </remarks>
		[XmlIgnore]
		[Browsable(false)]
		[DataMember]
		public IDictionary<string, object> ExtensionInfo
		{
			get => _extensionInfo;
			set
			{
				_extensionInfo = value ?? throw new ArgumentNullException(nameof(value));
				Notify(nameof(ExtensionInfo));
			}
		}

		[OnDeserialized]
		private void AfterDeserialization(StreamingContext ctx)
		{
			if (ExtensionInfo == null)
				ExtensionInfo = new Dictionary<string, object>();
		}

		[field: NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChanged += value;
			remove => _propertyChanged -= value;
		}

		private void Notify(string info)
		{
			_propertyChanged?.Invoke(this, info);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{0} ({1})".Put(Code, Exchange);
		}

		/// <summary>
		/// Compare <see cref="ExchangeBoard"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(ExchangeBoard other)
		{
			return Code == other.Code && Exchange == other.Exchange;
		}

		private int _hashCode;

		/// <summary>
		/// Get the hash code of the object <see cref="ExchangeBoard"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			if (_hashCode == 0)
				_hashCode = Code.GetHashCode() ^ (Exchange == null ? 0 : Exchange.GetHashCode());

			return _hashCode;
		}

		/// <summary>
		/// Create a copy of <see cref="ExchangeBoard"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override ExchangeBoard Clone()
		{
			return new ExchangeBoard
			{
				Exchange = Exchange,
				Code = Code,
				//IsSupportAtomicReRegister = IsSupportAtomicReRegister,
				//IsSupportMarketOrders = IsSupportMarketOrders,
				ExpiryTime = ExpiryTime,
				WorkingTime = WorkingTime.Clone(),
				TimeZone = TimeZone,
			};
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Exchange = storage.GetValue<SettingsStorage>(nameof(Exchange)).Load<Exchange>();
			Code = storage.GetValue<string>(nameof(Code));
			//IsSupportMarketOrders = storage.GetValue<bool>(nameof(IsSupportMarketOrders));
			//IsSupportAtomicReRegister = storage.GetValue<bool>(nameof(IsSupportAtomicReRegister));
			ExpiryTime = storage.GetValue<TimeSpan>(nameof(ExpiryTime));
			WorkingTime = storage.GetValue<SettingsStorage>(nameof(WorkingTime)).Load<WorkingTime>();
			TimeZone = storage.GetValue(nameof(TimeZone), TimeZone);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Exchange), Exchange.Save());
			storage.SetValue(nameof(Code), Code);
			//storage.SetValue(nameof(IsSupportMarketOrders), IsSupportMarketOrders);
			//storage.SetValue(nameof(IsSupportAtomicReRegister), IsSupportAtomicReRegister);
			storage.SetValue(nameof(ExpiryTime), ExpiryTime);
			storage.SetValue(nameof(WorkingTime), WorkingTime.Save());
			storage.SetValue(nameof(TimeZone), TimeZone);
		}
	}
}