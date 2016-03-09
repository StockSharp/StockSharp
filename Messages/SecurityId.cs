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

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Security ID.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public struct SecurityId : IEquatable<SecurityId>
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
			get { return _securityCode; }
			set { _securityCode = value; }
		}

		private string _boardCode;

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string BoardCode
		{
			get { return _boardCode; }
			set { _boardCode = value; }
		}

		private object _native;

		/// <summary>
		/// Native (internal) trading system security id.
		/// </summary>
		public object Native
		{
			get { return _native; }
			set { _native = value; }
		}

		private SecurityTypes? _securityType;

		/// <summary>
		/// Security type.
		/// </summary>
		public SecurityTypes? SecurityType
		{
			get { return _securityType; }
			set { _securityType = value; }
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
				_hashCode = _native?.GetHashCode() ?? (_securityCode + _boardCode).ToLowerInvariant().GetHashCode();
			}

			return _hashCode;
		}

		/// <summary>
		/// Compare <see cref="Currency"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		public override bool Equals(object other)
		{
			return Equals((SecurityId)other);
		}

		/// <summary>
		/// Compare <see cref="Currency"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		public bool Equals(SecurityId other)
		{
			if (EnsureGetHashCode() != other.EnsureGetHashCode())
				return false;

			if (_native == null)
				return _securityCode.CompareIgnoreCase(other._securityCode) && _boardCode.CompareIgnoreCase(other._boardCode);

			return _native.Equals(other.Native);
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

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return $"S#:{SecurityCode}@{BoardCode}, Native:{Native},Type:{SecurityType}";
		}
	}
}