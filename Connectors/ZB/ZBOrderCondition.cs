namespace StockSharp.ZB;

/// <summary>
/// <see cref="ZB"/> order condition.
/// </summary>
[Serializable]
[DataContract]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ZBKey)]
public class ZBOrderCondition : BaseWithdrawOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ZBOrderCondition"/>.
	/// </summary>
	public ZBOrderCondition()
	{
	}
}