namespace StockSharp.Configuration;

using System.Threading;

using Ecng.Common;

/// <summary>
/// Default implementation of <see cref="ICredentialsProvider"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultCredentialsProvider"/>.
/// </remarks>
/// <param name="fileSystem">File system. If null, uses <see cref="Paths.FileSystem"/>.</param>
/// <param name="credentialsFile">Credentials file path. If null, uses <see cref="Paths.CredentialsFile"/>.</param>
/// <param name="companyPath">Company directory path. If null, uses <see cref="Paths.CompanyPath"/>.</param>
public class DefaultCredentialsProvider(
	IFileSystem fileSystem = null,
	string credentialsFile = null,
	string companyPath = null) : ICredentialsProvider
{
	private readonly Lock _lock = new();
	private readonly IFileSystem _fileSystem = fileSystem ?? Paths.FileSystem;
	private readonly string _credentialsFile = credentialsFile ?? Paths.CredentialsFile;
	private readonly string _companyPath = companyPath ?? Paths.CompanyPath;

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

			credentials = null;

			try
			{
				if (_credentialsFile.IsConfigExists(_fileSystem))
				{
					credentials = new ServerCredentials();
					credentials.LoadIfNotNull(_credentialsFile.Deserialize<SettingsStorage>(_fileSystem));

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

			_fileSystem.CreateDirectory(_companyPath);

			var clone = credentials.Clone();
			if (!keepSecret)
				clone.Password = clone.Token = null;

			clone.Save().Serialize(_fileSystem, _credentialsFile);
		}
	}

	void ICredentialsProvider.Delete()
	{
		using (_lock.EnterScope())
		{
			if (_fileSystem.FileExists(_credentialsFile))
				_fileSystem.DeleteFile(_credentialsFile);

			_credentials = null;
		}
	}
}