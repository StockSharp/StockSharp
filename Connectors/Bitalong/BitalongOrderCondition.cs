namespace StockSharp.Bitalong;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

/// <summary>
/// <see cref="Bitalong"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BitalongKey)]
public class BitalongOrderCondition : BaseWithdrawOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BitalongOrderCondition"/>.
	/// </summary>
	public BitalongOrderCondition()
	{
	}
}