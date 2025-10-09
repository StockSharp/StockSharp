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
	IEnumerable<PermissionCredentials> Search(string loginPattern);

	/// <summary>
	/// Save credentials (add or update by login).
	/// </summary>
	/// <param name="credentials">Credentials to persist.</param>
	void Save(PermissionCredentials credentials);

	/// <summary>
	/// Delete credentials by login.
	/// </summary>
	/// <param name="login">Login.</param>
	/// <returns>Operation result.</returns>
	bool Delete(string login);
}