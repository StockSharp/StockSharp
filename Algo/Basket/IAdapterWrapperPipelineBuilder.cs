namespace StockSharp.Algo.Basket;

/// <summary>
/// Interface for building adapter wrapper pipeline.
/// </summary>
public interface IAdapterWrapperPipelineBuilder
{
	/// <summary>
	/// Creates wrapper pipeline around the specified adapter.
	/// </summary>
	/// <param name="adapter">The inner adapter to wrap.</param>
	/// <param name="config">Configuration for building the pipeline.</param>
	/// <returns>The outermost adapter in the pipeline (may be the original adapter if no wrappers applied).</returns>
	IMessageAdapter Build(IMessageAdapter adapter, AdapterWrapperConfiguration config);
}
