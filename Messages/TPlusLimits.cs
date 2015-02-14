namespace StockSharp.Messages
{
	using Ecng.ComponentModel;

	/// <summary>
	/// Виды лимитов для Т+ рынка.
	/// </summary>
	public enum TPlusLimits
	{
		/// <summary>
		/// Т+0.
		/// </summary>
		[EnumDisplayName("T+0")]
		T0,

		/// <summary>
		/// Т+1.
		/// </summary>
		[EnumDisplayName("T+1")]
		T1,

		/// <summary>
		/// Т+2.
		/// </summary>
		[EnumDisplayName("T+2")]
		T2
	}
}