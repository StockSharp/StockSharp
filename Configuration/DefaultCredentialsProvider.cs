namespace StockSharp.Configuration;

/// <summary>
/// Default implementation of <see cref="ICredentialsProvider"/>.
/// </summary>
public class DefaultCredentialsProvider : ICredentialsProvider
{
	private ServerCredentials _credentials;

	bool ICredentialsProvider.TryLoad(out ServerCredentials credentials)
	{
		lock (this)
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

		lock (this)
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

		lock (this)
		{
			if (File.Exists(fileName))
				File.Delete(fileName);

			_credentials = null;
		}
	}
}