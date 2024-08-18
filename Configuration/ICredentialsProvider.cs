namespace StockSharp.Configuration;

/// <summary>
/// Interface describing credentials provider.
/// </summary>
public interface ICredentialsProvider
{
	/// <summary>
	/// Try load credentials.
	/// </summary>
	/// <param name="credentials"><see cref="ServerCredentials"/>.</param>
	/// <returns>Operation result.</returns>
	bool TryLoad(out ServerCredentials credentials);

	/// <summary>
	/// Save credentials.
	/// </summary>
	/// <param name="credentials"><see cref="ServerCredentials"/>.</param>
	/// <param name="keepSecret">Save <see cref="ServerCredentials.Password"/> and <see cref="ServerCredentials.Token"/>.</param>
	void Save(ServerCredentials credentials, bool keepSecret);

	/// <summary>
	/// Delete credentials.
	/// </summary>
	void Delete();
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

	void ICredentialsProvider.Delete() => throw new NotSupportedException();
	void ICredentialsProvider.Save(ServerCredentials credentials, bool keepSecret) => throw new NotSupportedException();
	bool ICredentialsProvider.TryLoad(out ServerCredentials credentials)
	{
		credentials = new() { Token = _token };
		return true;
	}
}