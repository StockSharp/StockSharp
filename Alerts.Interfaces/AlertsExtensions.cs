namespace StockSharp.Alerts;

using Ecng.Configuration;

/// <summary>
/// Alert extensions.
/// </summary>
public static class AlertsExtensions
{
	/// <summary>
	/// Try to get registered Telegram channels.
	/// </summary>
	/// <returns>Collection of channels or <see langword="null"/> if service is not registered.</returns>
	public static IEnumerable<ITelegramChannel> TryGetChannels()
		=> ConfigManager.TryGetService<IEnumerable<ITelegramChannel>>();

	/// <summary>
	/// Register collection of Telegram channels for subsequent access through configuration container.
	/// </summary>
	/// <param name="channels">Collection of channels to register.</param>
	public static void RegisterChannels(IEnumerable<ITelegramChannel> channels)
		=> ConfigManager.RegisterService(channels);

	/// <summary>
	/// Try to find the channel by the specified identifier.
	/// </summary>
	/// <param name="channelId">Channel id.</param>
	/// <returns>Found channel or <see langword="null"/> if channel not found.</returns>
	public static ITelegramChannel TryFindChannel(this long channelId)
		=> TryGetChannels()?.FirstOrDefault(c => c.Id == channelId);
}