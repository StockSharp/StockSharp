namespace StockSharp.Studio
{
	using StockSharp.Studio.Core;

	public interface IContentWindow
	{
        string Id { get; }
		object Tag { get; }
		IStudioControl Control { get; }
	}
}