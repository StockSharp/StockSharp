namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// The quoting according to the Best By Volume rule. For this quoting the volume delta <see cref="VolumeExchange"/> is specified, which can stand in front of the quoted order.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public class BestByVolumeQuotingStrategy : QuotingStrategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BestByVolumeQuotingStrategy"/>.
	/// </summary>
	public BestByVolumeQuotingStrategy()
	{
		_volumeExchange = Param(nameof(VolumeExchange), new Unit());
	}

	private readonly StrategyParam<Unit> _volumeExchange;

	/// <summary>
	/// The volume delta that can stand in front of the quoted order.
	/// </summary>
	public Unit VolumeExchange
	{
		get => _volumeExchange.Value;
		set => _volumeExchange.Value = value;
	}

	/// <inheritdoc/>
	protected override IQuotingBehavior CreateBehavior()
		=> new BestByVolumeQuotingBehavior(VolumeExchange);
}