namespace StockSharp.Configuration;

/// <summary>
/// Interface describing credentials provider.
/// </summary>
public interface ICredentialsProvider
{
	/// <summary>
	/// Load the stored credentials.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The credentials, or <see langword="null"/> when none are stored.</returns>
	ValueTask<ServerCredentials> TryLoadAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Save credentials.
	/// </summary>
	/// <param name="credentials"><see cref="ServerCredentials"/>.</param>
	/// <param name="keepSecret">Save <see cref="ServerCredentials.Password"/> and <see cref="ServerCredentials.Token"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask SaveAsync(ServerCredentials credentials, bool keepSecret, CancellationToken cancellationToken);

	/// <summary>
	/// Delete credentials.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask DeleteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// In memory credentials provider.
/// </summary>
public class TokenCredentialsProvider : ICredentialsProvider
{
	private readonly SecureString _token;

	/// <summary>
	/// Initializes a new instance of the <see cref="TokenCredentialsProvider"/>.
	/// </summary>
	/// <param name="token">Token.</param>
	public TokenCredentialsProvider(string token)
		: this(token.ThrowIfEmpty(nameof(token)).Secure()) {}

	/// <summary>
	/// Initializes a new instance of the <see cref="TokenCredentialsProvider"/>.
	/// </summary>
	/// <param name="token">Token.</param>
	public TokenCredentialsProvider(SecureString token)
		=> _token = token.ThrowIfEmpty(nameof(token));

	ValueTask ICredentialsProvider.DeleteAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask ICredentialsProvider.SaveAsync(ServerCredentials credentials, bool keepSecret, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask<ServerCredentials> ICredentialsProvider.TryLoadAsync(CancellationToken cancellationToken)
		=> new(new ServerCredentials { Token = _token });
}
