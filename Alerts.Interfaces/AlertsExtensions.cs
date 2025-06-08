namespace StockSharp.Alerts;

using Ecng.Configuration;

/// <summary>
/// Alert extensions.
/// </summary>
public static class AlertsExtensions
{
	/// <summary>
	/// Try to find the channel by the specified identifier.
	/// </summary>
	/// <param name="channelId">Channel id.</param>
	/// <returns>Channel.</returns>
	public static ITelegramChannel TryFindChannel(this long channelId)
		=> ConfigManager
		.TryGetService<IEnumerable<ITelegramChannel>>()?
		.FirstOrDefault(c => c.Id == channelId);
}