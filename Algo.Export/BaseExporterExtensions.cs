namespace StockSharp.Algo.Export;

using System.Collections;

/// <summary>
/// Extensions for <see cref="BaseExporter"/>.
/// </summary>
public static class BaseExporterExtensions
{
	/// <summary>
	/// To export values.
	/// </summary>
	/// <param name="exporter"><see cref="BaseExporter"/></param>
	/// <param name="values">Value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Count and last time.</returns>
	[Obsolete("Use Export(IAsyncEnumerable<object>, CancellationToken) method.")]
	public static Task<(int, DateTime?)> Export(this BaseExporter exporter, IEnumerable values, CancellationToken cancellationToken)
	{
		if (values == null)
			throw new ArgumentNullException(nameof(values));

		return exporter.Export(values.Cast<object>().ToAsyncEnumerable(), cancellationToken);
	}
}