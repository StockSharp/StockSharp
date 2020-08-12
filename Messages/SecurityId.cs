#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: SecurityId.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Globalization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Security ID.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public struct SecurityId : IEquatable<SecurityId>, IPersistable
	{
		private string _securityCode;

		/// <summary>
		/// Security code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str349Key)]
		[DescriptionLoc(LocalizedStrings.Str349Key, true)]
		[MainCategory]
		public string SecurityCode
		{
			get => _securityCode;
			set => _securityCode = value;
		}

		private string _boardCode;

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey, true)]
		[MainCategory]
		public string BoardCode
		{
			get => _boardCode;
			set => _boardCode = value;
		}

		private object _native;

		/// <summary>
		/// Native (internal) trading system security id.
		/// </summary>
		public object Native
		{
			get => _nativeAsInt != 0 ? _nativeAsInt : _native;
			set
			{
				_native = value;

				_nativeAsInt = 0;

				if (value is long l)
					_nativeAsInt = l;
			}
		}

		private long _nativeAsInt;

		/// <summary>
		/// Native (internal) trading system security id represented as integer.
		/// </summary>
		public long NativeAsInt
		{
			get => _nativeAsInt;
			set => _nativeAsInt = value;
		}

		private SecurityTypes? _securityType;

		/// <summary>
		/// Security type.
		/// </summary>
		[Obsolete]
		public SecurityTypes? SecurityType
		{
			get => _securityType;
			set => _securityType = value;
		}

		/// <summary>
		/// ID in SEDOL format (Stock Exchange Daily Official List).
		/// </summary>
		[DataMember]
		[DisplayName("SEDOL")]
		[DescriptionLoc(LocalizedStrings.Str351Key)]
		public string Sedol { get; set; }

		/// <summary>
		/// ID in CUSIP format (Committee on Uniform Securities Identification Procedures).
		/// </summary>
		[DataMember]
		[DisplayName("CUSIP")]
		[DescriptionLoc(LocalizedStrings.Str352Key)]
		public string Cusip { get; set; }

		/// <summary>
		/// ID in ISIN format (International Securities Identification Number).
		/// </summary>
		[DataMember]
		[DisplayName("ISIN")]
		[DescriptionLoc(LocalizedStrings.Str353Key)]
		public string Isin { get; set; }

		/// <summary>
		/// ID in RIC format (Reuters Instrument Code).
		/// </summary>
		[DataMember]
		[DisplayName("RIC")]
		[DescriptionLoc(LocalizedStrings.Str354Key)]
		public string Ric { get; set; }

		/// <summary>
		/// ID in Bloomberg format.
		/// </summary>
		[DataMember]
		[DisplayName("Bloomberg")]
		[DescriptionLoc(LocalizedStrings.Str355Key)]
		public string Bloomberg { get; set; }

		/// <summary>
		/// ID in IQFeed format.
		/// </summary>
		[DataMember]
		[DisplayName("IQFeed")]
		[DescriptionLoc(LocalizedStrings.Str356Key)]
		public string IQFeed { get; set; }

		/// <summary>
		/// ID in Interactive Brokers format.
		/// </summary>
		[DataMember]
		[DisplayName("InteractiveBrokers")]
		[DescriptionLoc(LocalizedStrings.Str357Key)]
		[Nullable]
		public int? InteractiveBrokers { get; set; }

		/// <summary>
		/// ID in Plaza format.
		/// </summary>
		[DataMember]
		[DisplayName("Plaza")]
		[DescriptionLoc(LocalizedStrings.Str358Key)]
		public string Plaza { get; set; }

		private int _hashCode;

		/// <summary>
		/// Get the hash code of the object.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return EnsureGetHashCode();
		}

		private int EnsureGetHashCode()
		{
			if (_hashCode == 0)
			{
				_hashCode = (_nativeAsInt != 0 ? _nativeAsInt.GetHashCode() : _native?.GetHashCode())
				            ?? (_securityCode + _boardCode).ToLowerInvariant().GetHashCode();
			}

			return _hashCode;
		}

		/// <summary>
		/// Compare <see cref="SecurityId"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		public override bool Equals(object other)
		{
			return other is SecurityId secId && Equals(secId);
		}

		/// <summary>
		/// Compare <see cref="SecurityId"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		public bool Equals(SecurityId other)
		{
			if (EnsureGetHashCode() != other.EnsureGetHashCode())
				return false;

			if (_nativeAsInt != 0)
				return _nativeAsInt.Equals(other._nativeAsInt);

			if (_native != null)
				return _native.Equals(other._native);

			return _securityCode.CompareIgnoreCase(other._securityCode) && _boardCode.CompareIgnoreCase(other._boardCode);
		}

		/// <summary>
		/// Compare the inequality of two identifiers.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <returns><see langword="true" />, if identifiers are equal, otherwise, <see langword="false" />.</returns>
		public static bool operator !=(SecurityId left, SecurityId right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Compare two identifiers for equality.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <returns><see langword="true" />, if the specified identifiers are equal, otherwise, <see langword="false" />.</returns>
		public static bool operator ==(SecurityId left, SecurityId right)
		{
			return left.Equals(right);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var id = $"{SecurityCode}@{BoardCode}";

			if (Native != null)
				id += $",Native:{Native}";

			//if (SecurityType != null)
			//	id += $",Type:{SecurityType.Value}";

			if (!Isin.IsEmpty())
				id += $",ISIN:{Isin}";

			if (!IQFeed.IsEmpty())
				id += $",IQFeed:{IQFeed}";

			if (InteractiveBrokers != null)
				id += $",IB:{InteractiveBrokers}";

			return id;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			SecurityCode = storage.GetValue<string>(nameof(SecurityCode));
			BoardCode = storage.GetValue<string>(nameof(BoardCode));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(SecurityCode), SecurityCode);
			storage.SetValue(nameof(BoardCode), BoardCode);
		}

		/// <summary>
		/// Board code for combined security.
		/// </summary>
		public const string AssociatedBoardCode = "ALL";

		/// <summary>
		/// Create security id with board code set as <see cref="AssociatedBoardCode"/>.
		/// </summary>
		/// <param name="securityCode">Security code.</param>
		/// <returns>Security ID.</returns>
		public static SecurityId CreateAssociated(string securityCode)
		{
			return new SecurityId
			{
				SecurityCode = securityCode,
				BoardCode = AssociatedBoardCode,
			};
		}

		/// <summary>
		/// "Money" security id.
		/// </summary>
		public static readonly SecurityId Money = new SecurityId
		{
			SecurityCode = "MONEY",
			BoardCode = AssociatedBoardCode
		};

		/// <summary>
		/// "News" security id.
		/// </summary>
		public static readonly SecurityId News = new SecurityId
		{
			SecurityCode = "NEWS",
			BoardCode = AssociatedBoardCode
		};
	}

	/// <summary>
	/// Converter to use with <see cref="SecurityId"/> properties.
	/// </summary>
	public class StringToSecurityIdTypeConverter : TypeConverter
	{
		/// <inheritdoc />
		public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type sourceType)
			=> sourceType == typeof(string) || base.CanConvertFrom(ctx, sourceType);

		/// <inheritdoc />
		public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo culture, object value)
		{
			if(!(value is string securityId))
				return base.ConvertFrom(ctx, culture, value);

			var isNullable = ctx.PropertyDescriptor?.PropertyType.IsNullable() == true;

			const string delimiter = "@";
			var index = securityId.LastIndexOfIgnoreCase(delimiter);
			return index < 0 ?
				isNullable ? (SecurityId?)null : default(SecurityId) :
				new SecurityId { SecurityCode = securityId.Substring(0, index), BoardCode = securityId.Substring(index + delimiter.Length, securityId.Length - index - delimiter.Length) };
		}
	}

}