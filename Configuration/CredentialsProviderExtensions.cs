namespace StockSharp.Configuration;

/// <summary>
/// Extension class for <see cref="ICredentialsProvider"/>.
/// </summary>
public static class CredentialsProviderExtensions
{
	/// <summary>
	/// Try load credentials.
	/// </summary>
	/// <param name="provider"><see cref="ICredentialsProvider"/></param>
	/// <param name="credentials"><see cref="ServerCredentials"/>.</param>
	/// <returns>Whether the credentials are enough to log in without asking.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use TryLoadAsync instead.")]
	public static bool TryLoad(this ICredentialsProvider provider, out ServerCredentials credentials)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		credentials = AsyncHelper.Run(() => provider.TryLoadAsync(default));
		return credentials?.CanAutoLogin() == true;
	}

	/// <summary>
	/// Save credentials.
	/// </summary>
	/// <param name="provider"><see cref="ICredentialsProvider"/></param>
	/// <param name="credentials"><see cref="ServerCredentials"/>.</param>
	/// <param name="keepSecret">Save secret information.</param>
	[Obsolete("Blocking sync-over-async wrapper. Use SaveAsync instead.")]
	public static void Save(this ICredentialsProvider provider, ServerCredentials credentials, bool keepSecret)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		AsyncHelper.Run(() => provider.SaveAsync(credentials, keepSecret, default));
	}

	/// <summary>
	/// Delete credentials.
	/// </summary>
	/// <param name="provider"><see cref="ICredentialsProvider"/></param>
	[Obsolete("Blocking sync-over-async wrapper. Use DeleteAsync instead.")]
	public static void Delete(this ICredentialsProvider provider)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		AsyncHelper.Run(() => provider.DeleteAsync(default));
	}

	internal static async ValueTask<T> CopyAsync<T>(this T credentials, CancellationToken cancellationToken)
		where T : ServerCredentials
	{
		var copy = credentials.GetType().CreateInstance<T>();
		await copy.LoadAsync(await credentials.SaveAsync(cancellationToken), cancellationToken);
		return copy;
	}
}