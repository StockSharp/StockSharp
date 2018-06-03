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
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

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
			ExtensionInfo = new Dictionary<string, object>();
			RusName = EngName = string.Empty;
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
				Notify(nameof(Name));
			}
		}

		private string _rusName;

		/// <summary>
		/// Russian exchange name.
		/// </summary>
		[DataMember]
		public string RusName
		{
			get => _rusName;
			set
			{
				if (RusName == value)
					return;

				_rusName = value;
				Notify(nameof(RusName));
			}
		}

		private string _engName;

		/// <summary>
		/// English exchange name.
		/// </summary>
		[DataMember]
		public string EngName
		{
			get => _engName;
			set
			{
				if (EngName == value)
					return;

				_engName = value;
				Notify(nameof(EngName));
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
				Notify(nameof(CountryCode));
			}
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
			return Name;
		}

		/// <summary>
		/// Compare <see cref="Exchange"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(Exchange other)
		{
			return Name == other.Name;
		}

		/// <summary>
		/// Get the hash code of the object <see cref="Exchange"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		/// <summary>
		/// Create a copy of <see cref="Exchange"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Exchange Clone()
		{
			return new Exchange
			{
				Name = Name,
				RusName = RusName,
				EngName = EngName,
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
			RusName = storage.GetValue<string>(nameof(RusName));
			EngName = storage.GetValue<string>(nameof(EngName));
			CountryCode = storage.GetValue<CountryCodes?>(nameof(CountryCode));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Name), Name);
			storage.SetValue(nameof(RusName), RusName);
			storage.SetValue(nameof(EngName), EngName);
			storage.SetValue(nameof(CountryCode), CountryCode.To<string>());
		}
	}
}