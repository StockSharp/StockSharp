namespace StockSharp.Configuration.Permissions;

/// <summary>
/// Extensions.
/// </summary>
public static class PermissionCredentialsExtensions
{
	/// <summary>
	/// Convert <see cref="UserInfoMessage"/> to <see cref="PermissionCredentials"/> value.
	/// </summary>
	/// <param name="message">The message contains information about user.</param>
	/// <returns>Credentials with set of permissions.</returns>
	public static PermissionCredentials ToCredentials(this UserInfoMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var credentials = new PermissionCredentials
		{
			Email = message.Login,
			Password = message.Password,
			IpRestrictions = [.. message.IpRestrictions]
		};

		foreach (var permission in message.Permissions)
		{
			var dict = new SynchronizedDictionary<(string, string, string, DateTime?), bool>();
			dict.AddRange(permission.Value);
			credentials.Permissions.Add(permission.Key, dict);
		}

		return credentials;
	}

	/// <summary>
	/// Convert <see cref="PermissionCredentials"/> to <see cref="UserInfoMessage"/> value.
	/// </summary>
	/// <param name="credentials">Credentials with set of permissions.</param>
	/// <param name="copyPassword">Copy <see cref="ServerCredentials.Password"/> value.</param>
	/// <returns>The message contains information about user.</returns>
	public static UserInfoMessage ToUserInfoMessage(this PermissionCredentials credentials, bool copyPassword)
	{
		if (credentials == null)
			throw new ArgumentNullException(nameof(credentials));

		var message = new UserInfoMessage
		{
			Login = credentials.Email,
			IpRestrictions = [.. credentials.IpRestrictions],
		};

		if (copyPassword)
			message.Password = credentials.Password;

		foreach (var permission in credentials.Permissions)
		{
			message.Permissions.Add(permission.Key, permission.Value.ToDictionary());
		}

		return message;
	}

	/// <summary>
	/// Get all credentials.
	/// </summary>
	/// <param name="storage">The storage to be used.</param>
	/// <returns>All stored credentials.</returns>
	public static IEnumerable<PermissionCredentials> GetAll(this IPermissionCredentialsStorage storage)
		=> storage.CheckOnNull(nameof(storage)).Search("*");

	/// <summary>
	/// Find credentials by exact login.
	/// </summary>
	/// <param name="storage">Credentials storage.</param>
	/// <param name="login">Login.</param>
	/// <returns>Credentials with permissions or <c>null</c> if not found.</returns>
	public static PermissionCredentials TryGetByLogin(this IPermissionCredentialsStorage storage, string login)
	{
		storage.CheckOnNull(nameof(storage));
		login.ThrowIfEmpty(nameof(login));

		// build exact-match pattern by escaping '*' and other wildcards
		var pattern = login
			.Replace("\\", "\\\\")
			.Replace("*", "\\*");

		return storage.Search(pattern).FirstOrDefault(c => c.Email.EqualsIgnoreCase(login));
	}
}