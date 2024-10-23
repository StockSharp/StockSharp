namespace StockSharp.Algo.Strategies.Reporting;

/// <summary>
/// <see cref="IReportGenerator"/> provider.
/// </summary>
public interface IReportGeneratorProvider
{
	/// <summary>
	/// Available generators.
	/// </summary>
	IEnumerable<IReportGenerator> Generators { get; }
}

/// <summary>
/// Default implementation <see cref="IReportGeneratorProvider"/>.
/// </summary>
public class ReportGeneratorProvider : IReportGeneratorProvider
{
	private readonly HashSet<IReportGenerator> _generators = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="ReportGeneratorProvider"/>.
	/// </summary>
	/// <param name="generators">Available generators.</param>
	public ReportGeneratorProvider(IEnumerable<IReportGenerator> generators)
	{
		_generators.AddRange(generators ?? throw new ArgumentNullException(nameof(generators)));
	}

	IEnumerable<IReportGenerator> IReportGeneratorProvider.Generators
		=> _generators;
}