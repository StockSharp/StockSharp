namespace StockSharp.DarkHorse;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

/// <summary>
/// <see cref="DarkHorse"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BtceKey)]
public class DarkHorseCondition : BaseWithdrawOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DarkHorseCondition"/>.
	/// </summary>
	public DarkHorseCondition()
	{
	}
}