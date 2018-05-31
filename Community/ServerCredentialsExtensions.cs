namespace StockSharp.Community
{
	using System;
	using System.IO;

	using Ecng.Serialization;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class ServerCredentialsExtensions
	{
		/// <summary>
		/// The StockSharp folder in <see cref="Environment.SpecialFolder.MyDocuments"/> location.
		/// </summary>
		public static string StockSharpFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockSharp");

		private const string _credentialsFile = "credentials.xml";

		/// <summary>
		/// Try load credentials from <see cref="StockSharpFolder"/>.
		/// </summary>
		/// <param name="credentials">The class that contains a login and password to access the services https://stocksharp.com .</param>
		/// <returns><see langword="true"/> if the specified credentials was loaded successfully, otherwise, <see langword="false"/>.</returns>
		public static bool TryLoadCredentials(this ServerCredentials credentials)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			var file = Path.Combine(StockSharpFolder, _credentialsFile);

			if (!File.Exists(file))
				return false;

			credentials.Load(new XmlSerializer<SettingsStorage>().Deserialize(file));
			return true;
		}

		/// <summary>
		/// Save the credentials to <see cref="StockSharpFolder"/>.
		/// </summary>
		/// <param name="credentials">The class that contains a login and password to access the services https://stocksharp.com .</param>
		public static void SaveCredentials(this ServerCredentials credentials)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			credentials.SaveCredentials(credentials.AutoLogon);
		}

		/// <summary>
		/// Save the credentials to <see cref="StockSharpFolder"/>.
		/// </summary>
		/// <param name="credentials">The class that contains a login and password to access the services https://stocksharp.com .</param>
		/// <param name="savePassword">Save password.</param>
		public static void SaveCredentials(this ServerCredentials credentials, bool savePassword)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			var clone = credentials;

			if (!savePassword)
				clone.Password = null;

			var file = Path.Combine(StockSharpFolder, _credentialsFile);

			new XmlSerializer<SettingsStorage>().Serialize(clone.Save(), file);
		}
	}
}