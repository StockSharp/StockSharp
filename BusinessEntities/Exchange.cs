#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: Exchange.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Exchange info.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[KnownType(typeof(TimeZoneInfo))]
	[KnownType(typeof(TimeZoneInfo.AdjustmentRule))]
	[KnownType(typeof(TimeZoneInfo.AdjustmentRule[]))]
	[KnownType(typeof(TimeZoneInfo.TransitionTime))]
	[KnownType(typeof(DayOfWeek))]
	public partial class Exchange : Equatable<Exchange>, IExtendableEntity, IPersistable, INotifyPropertyChanged
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Exchange"/>.
		/// </summary>
		public Exchange()
		{
		}

		private string _name;

		/// <summary>
		/// Exchange code name.
		/// </summary>
		[DataMember]
		[Identity]
		public string Name
		{
			get => _name;
			set
			{
				if (Name == value)
					return;

				_name = value;
				Notify();
			}
		}

		private string GetLocName(Languages? language) => FullNameLoc.IsEmpty() ? null : LocalizedStrings.GetString(FullNameLoc, language);

		/// <summary>
		/// Russian exchange name.
		/// </summary>
		[Obsolete]
		public string RusName => GetLocName(Languages.Russian);

		/// <summary>
		/// English exchange name.
		/// </summary>
		[Obsolete]
		public string EngName => GetLocName(Languages.English);

		/// <summary>
		/// Full name.
		/// </summary>
		public string FullName => GetLocName(null);

		private string _fullNameLoc;

		/// <summary>
		/// Full name (localization key).
		/// </summary>
		[DataMember]
		public string FullNameLoc
		{
			get => _fullNameLoc;
			set
			{
				if (FullNameLoc == value)
					return;

				_fullNameLoc = value;
				Notify();
			}
		}

		private CountryCodes? _countryCode;

		/// <summary>
		/// ISO country code.
		/// </summary>
		[DataMember]
		[Nullable]
		public CountryCodes? CountryCode
		{
			get => _countryCode;
			set
			{
				if (CountryCode == value)
					return;

				_countryCode = value;
				Notify();
			}
		}

		[field: NonSerialized]
		private IDictionary<string, object> _extensionInfo/* = new Dictionary<string, object>()*/;

		/// <inheritdoc />
		[XmlIgnore]
		[Browsable(false)]
		[DataMember]
		[Obsolete]
		public IDictionary<string, object> ExtensionInfo
		{
			get => _extensionInfo;
			set
			{
				_extensionInfo = value/* ?? throw new ArgumentNullException(nameof(value))*/;
				Notify();
			}
		}

		//[OnDeserialized]
		//private void AfterDeserialization(StreamingContext ctx)
		//{
		//	if (ExtensionInfo == null)
		//		ExtensionInfo = new Dictionary<string, object>();
		//}

		[field: NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChanged += value;
			remove => _propertyChanged -= value;
		}

		private void Notify([CallerMemberName]string propertyName = null)
		{
			_propertyChanged?.Invoke(this, propertyName);
		}

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <summary>
		/// Compare <see cref="Exchange"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(Exchange other)
		{
			return Name == other.Name;
		}

		/// <summary>Serves as a hash function for a particular type. </summary>
		/// <returns>A hash code for the current <see cref="T:System.Object" />.</returns>
		public override int GetHashCode() => Name.GetHashCode();

		/// <summary>
		/// Create a copy of <see cref="Exchange"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Exchange Clone()
		{
			return new Exchange
			{
				Name = Name,
				FullNameLoc = FullNameLoc,
				CountryCode = CountryCode,
			};
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Name = storage.GetValue<string>(nameof(Name));
			FullNameLoc = storage.GetValue<string>(nameof(FullNameLoc));
			CountryCode = storage.GetValue<CountryCodes?>(nameof(CountryCode));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Name), Name);
			storage.SetValue(nameof(FullNameLoc), FullNameLoc);
			storage.SetValue(nameof(CountryCode), CountryCode.To<string>());
		}
	}
}