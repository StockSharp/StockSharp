namespace StockSharp.Configuration;

/// <summary>
/// Services registry.
/// </summary>
public static class ConfigurationServicesRegistry
{
	/// <summary>
	/// <see cref="ICredentialsProvider"/>.
	/// </summary>
	public static ICredentialsProvider TryCredentialsProvider => ConfigManager.TryGetService<ICredentialsProvider>();

	/// <summary>
	/// <see cref="ICredentialsProvider"/>.
	/// </summary>
	public static ICredentialsProvider CredentialsProvider => ConfigManager.GetService<ICredentialsProvider>();
}