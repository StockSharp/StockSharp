namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.ComponentModel;

	/// <summary>
	/// Виды лимитов для Т+ рынка.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum TPlusLimits
	{
		/// <summary>
		/// Т+0.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("T+0")]
		T0,

		/// <summary>
		/// Т+1.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("T+1")]
		T1,

		/// <summary>
		/// Т+2.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("T+2")]
		T2
	}
}