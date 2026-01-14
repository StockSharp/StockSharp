namespace StockSharp.Configuration.Permissions;

/// <summary>
/// Abstraction for permission credentials storage.
/// </summary>
public interface IPermissionCredentialsStorage
{
	/// <summary>
	/// Find credentials by login pattern. Use '*' to match any sequence; pass "*" to return all.
	/// </summary>
	/// <param name="loginPattern">Login pattern (supports '*').</param>
	/// <returns>Matched credentials.</returns>
	IAsyncEnumerable<PermissionCredentials> SearchAsync(string loginPattern);

	/// <summary>
	/// Save credentials (add or update by login).
	/// </summary>
	/// <param name="credentials">Credentials to persist.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask SaveAsync(PermissionCredentials credentials, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete credentials by login.
	/// </summary>
	/// <param name="login">Login.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Operation result.</returns>
	ValueTask<bool> DeleteAsync(string login, CancellationToken cancellationToken = default);
}