namespace StockSharp.Bibox;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

/// <summary>
/// <see cref="Bibox"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BiboxKey)]
public class BiboxOrderCondition : BaseWithdrawOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BiboxOrderCondition"/>.
	/// </summary>
	public BiboxOrderCondition()
	{
	}
}