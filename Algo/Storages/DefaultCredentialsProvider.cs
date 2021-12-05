namespace StockSharp.Algo.Storages
{
	using System;
	using System.IO;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Configuration;

	/// <summary>
	/// Default implementation of <see cref="ICredentialsProvider"/>.
	/// </summary>
	public class DefaultCredentialsProvider : ICredentialsProvider
	{
		private const string _credentialsFile = $"credentials{Paths.DefaultSettingsExt}";

		bool ICredentialsProvider.TryLoad(out ServerCredentials credentials)
		{
			var file = Path.Combine(Paths.CompanyPath, _credentialsFile);

			if (!File.Exists(file) && !File.Exists(file.MakeLegacy()))
			{
				credentials = null;
				return false;
			}

			credentials = new ServerCredentials();
			credentials.LoadIfNotNull(file.DeserializeWithMigration<SettingsStorage>());
			return true;
		}

		void ICredentialsProvider.Save(ServerCredentials credentials)
		{
			if (credentials is null)
				throw new ArgumentNullException(nameof(credentials));

			var clone = credentials;

			if (!credentials.AutoLogon)
				clone.Password = null;

			Directory.CreateDirectory(Paths.CompanyPath);

			var file = Path.Combine(Paths.CompanyPath, _credentialsFile);

			clone.Save().Serialize(file);
		}
	}
}