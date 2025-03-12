namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	/// <inheritdoc />
	public event Action<TimeSpan> CurrentTimeChanged;

	private void OnConnectorCurrentTimeChanged(TimeSpan diff)
		=> CurrentTimeChanged?.Invoke(diff);
}