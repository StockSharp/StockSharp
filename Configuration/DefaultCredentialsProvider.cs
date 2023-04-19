namespace StockSharp.Configuration
{
	using System;
	using System.IO;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Logging;

	/// <summary>
	/// Default implementation of <see cref="ICredentialsProvider"/>.
	/// </summary>
	public class DefaultCredentialsProvider : ICredentialsProvider
	{
		private static readonly string _credentialsFile = $"credentials{Paths.DefaultSettingsExt}";

		private ServerCredentials _credentials;

		// ReSharper disable InconsistentlySynchronizedField
		private bool IsValid => _credentials != null && (!_credentials.Password.IsEmpty() || !_credentials.Token.IsEmpty());
		// ReSharper restore InconsistentlySynchronizedField

		private static string FileName => Path.Combine(Paths.CompanyPath, _credentialsFile);

		bool ICredentialsProvider.TryLoad(out ServerCredentials credentials)
		{
			lock (this)
			{
				if(_credentials != null)
				{
					credentials = _credentials.Clone();
					return IsValid;
				}

				var file = FileName;
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

				return IsValid;
			}
		}

		void ICredentialsProvider.Save(ServerCredentials credentials)
		{
			if (credentials is null)
				throw new ArgumentNullException(nameof(credentials));

			lock (this)
			{
				var clone = credentials.Clone();

				if (!clone.AutoLogon)
					clone.Password = null;

				Directory.CreateDirectory(Paths.CompanyPath);

				clone.Save().Serialize(FileName);

				_credentials = clone;
			}
		}

		void ICredentialsProvider.Delete()
		{
			var fileName = FileName;

			lock (this)
			{
				if (File.Exists(fileName))
					File.Delete(fileName);
			}
		}
	}
}