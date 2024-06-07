namespace StockSharp.Btce;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

/// <summary>
/// <see cref="Btce"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BtceKey)]
public class BtceOrderCondition : BaseWithdrawOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BtceOrderCondition"/>.
	/// </summary>
	public BtceOrderCondition()
	{
	}
}