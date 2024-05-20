namespace StockSharp.Bitexbook
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// <see cref="Bitexbook"/> order condition.
	/// </summary>
	[Serializable]
	[DataContract]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BitexbookKey)]
	public class BitexbookOrderCondition : BaseWithdrawOrderCondition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BitexbookOrderCondition"/>.
		/// </summary>
		public BitexbookOrderCondition()
		{
		}
	}
}