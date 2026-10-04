namespace StockSharp.Configuration;

using System.Threading;

using Ecng.Common;

using Nito.AsyncEx;

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
	private readonly AsyncLock _lock = new();
	private readonly IFileSystem _fileSystem = fileSystem ?? Paths.FileSystem;
	private readonly string _credentialsFile = credentialsFile ?? Paths.CredentialsFile;
	private readonly string _companyPath = companyPath ?? Paths.CompanyPath;

	private ServerCredentials _credentials;

	async ValueTask<ServerCredentials> ICredentialsProvider.TryLoadAsync(CancellationToken cancellationToken)
	{
		using (await _lock.LockAsync(cancellationToken))
		{
			if (_credentials != null)
				return await _credentials.CopyAsync(cancellationToken);

			try
			{
				if (!_credentialsFile.IsConfigExists(_fileSystem))
					return null;

				var credentials = new ServerCredentials();
				await credentials.LoadIfNotNullAsync(await _credentialsFile.DeserializeAsync<SettingsStorage>(_fileSystem, cancellationToken), cancellationToken);

				_credentials = await credentials.CopyAsync(cancellationToken);

				return credentials;
			}
			catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
			{
				ex.LogError();
				return null;
			}
		}
	}

	async ValueTask ICredentialsProvider.SaveAsync(ServerCredentials credentials, bool keepSecret, CancellationToken cancellationToken)
	{
		if (credentials is null)
			throw new ArgumentNullException(nameof(credentials));

		using (await _lock.LockAsync(cancellationToken))
		{
			_credentials = await credentials.CopyAsync(cancellationToken);

			_fileSystem.CreateDirectory(_companyPath);

			var clone = await credentials.CopyAsync(cancellationToken);
			if (!keepSecret)
				clone.Password = clone.Token = null;

			await (await clone.SaveAsync(cancellationToken)).SerializeAsync(_fileSystem, _credentialsFile, true, cancellationToken);
		}
	}

	async ValueTask ICredentialsProvider.DeleteAsync(CancellationToken cancellationToken)
	{
		using (await _lock.LockAsync(cancellationToken))
		{
			if (_fileSystem.FileExists(_credentialsFile))
				_fileSystem.DeleteFile(_credentialsFile);

			_credentials = null;
		}
	}
}