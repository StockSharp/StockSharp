namespace StockSharp.Bittrex;

/// <summary>
/// <see cref="Bittrex"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BittrexKey)]
public class BittrexOrderCondition : BaseWithdrawOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BittrexOrderCondition"/>.
	/// </summary>
	public BittrexOrderCondition()
	{
	}
}