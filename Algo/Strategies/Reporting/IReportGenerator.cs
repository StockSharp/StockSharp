namespace StockSharp.Algo.Strategies.Reporting;

/// <summary>
/// The interface describe report generator for strategies.
/// </summary>
public interface IReportGenerator
{
	/// <summary>
	/// Name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Extension without leading dot char.
	/// </summary>
	string Extension { get; }

	/// <summary>
	/// To add <see cref="Order"/> to the report. <see cref="Order"/> are added by default.
	/// </summary>
	bool IncludeOrders { get; set; }

	/// <summary>
	/// To add <see cref="MyTrade"/> to the report. <see cref="MyTrade"/> are added by default.
	/// </summary>
	bool IncludeTrades { get; set; }

	/// <summary>
	/// To generate the report.
	/// </summary>
	/// <param name="strategy"><see cref="Strategy"/>.</param>
	/// <param name="fileName">The name of the file, in which the report is generated.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	ValueTask Generate(Strategy strategy, string fileName, CancellationToken cancellationToken = default);
}

/// <summary>
/// The base report generator for strategies.
/// </summary>
public abstract class BaseReportGenerator : IReportGenerator
{
	/// <summary>
	/// Initialize <see cref="BaseReportGenerator"/>.
	/// </summary>
	protected BaseReportGenerator()
	{
	}

	/// <inheritdoc />
	public bool IncludeOrders { get; set; } = true;

	/// <inheritdoc />
	public bool IncludeTrades { get; set; } = true;

	/// <inheritdoc />
	public abstract string Name { get; }

	/// <inheritdoc />
	public abstract string Extension { get; }

	/// <inheritdoc />
	public abstract ValueTask Generate(Strategy strategy, string fileName, CancellationToken cancellationToken);
}