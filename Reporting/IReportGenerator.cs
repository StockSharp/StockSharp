namespace StockSharp.Reporting;

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
	/// To add orders to the report. Orders are added by default.
	/// </summary>
	bool IncludeOrders { get; set; }

	/// <summary>
	/// To add trades to the report. Trades are added by default.
	/// </summary>
	bool IncludeTrades { get; set; }

	/// <summary>
	/// Encoding.
	/// </summary>
	Encoding Encoding { get; set; }

	/// <summary>
	/// To generate the report.
	/// </summary>
	/// <param name="source"><see cref="IReportSource"/>.</param>
	/// <param name="stream">The stream to write the report to.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	ValueTask Generate(IReportSource source, Stream stream, CancellationToken cancellationToken);
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

	private Encoding _encoding = Encoding.UTF8;

	/// <inheritdoc />
	public Encoding Encoding
	{
		get => _encoding;
		set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	public abstract string Name { get; }

	/// <inheritdoc />
	public abstract string Extension { get; }

	/// <inheritdoc />
	public ValueTask Generate(IReportSource source, Stream stream, CancellationToken cancellationToken)
	{
		source.Prepare();
		return OnGenerate(source, stream, cancellationToken);
	}

	/// <summary>
	/// Generates the report content.
	/// </summary>
	/// <param name="source"><see cref="IReportSource"/>.</param>
	/// <param name="stream">The stream to write the report to.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	protected abstract ValueTask OnGenerate(IReportSource source, Stream stream, CancellationToken cancellationToken);
}