namespace StockSharp.Configuration;

using System.Threading;

/// <summary>
/// Default implementation of <see cref="ICredentialsProvider"/>.
/// </summary>
public class DefaultCredentialsProvider : ICredentialsProvider
{
	private readonly Lock _lock = new();

	private ServerCredentials _credentials;

	bool ICredentialsProvider.TryLoad(out ServerCredentials credentials)
	{
		using (_lock.EnterScope())
		{
			if(_credentials != null)
			{
				credentials = _credentials.Clone();
				return credentials.CanAutoLogin();
			}

			var file = Paths.CredentialsFile;
			credentials = null;

			try
			{
				if (file.IsConfigExists())
				{
					credentials = new ServerCredentials();
					credentials.LoadIfNotNull(file.Deserialize<SettingsStorage>());

					_credentials = credentials.Clone();
				}
			}
			catch (Exception ex)
			{
				ex.LogError();
			}

			return credentials?.CanAutoLogin() == true;
		}
	}

	void ICredentialsProvider.Save(ServerCredentials credentials, bool keepSecret)
	{
		if (credentials is null)
			throw new ArgumentNullException(nameof(credentials));

		using (_lock.EnterScope())
		{
			_credentials = credentials.Clone();

			Directory.CreateDirectory(Paths.CompanyPath);

			var clone = credentials.Clone();
			if (!keepSecret)
				clone.Password = clone.Token = null;

			clone.Save().Serialize(Paths.CredentialsFile);
		}
	}

	void ICredentialsProvider.Delete()
	{
		var fileName = Paths.CredentialsFile;

		using (_lock.EnterScope())
		{
			if (File.Exists(fileName))
				File.Delete(fileName);

			_credentials = null;
		}
	}
}