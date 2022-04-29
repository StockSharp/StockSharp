namespace StockSharp.Configuration
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Invariant culture <see cref="ISerializer"/>.
	/// </summary>
	public static class InvariantCultureSerializer
	{
		/// <summary>
		/// Serialize the specified storage into file.
		/// </summary>
		/// <param name="settings"><see cref="SettingsStorage"/></param>
		/// <param name="fileName">File name.</param>
		public static void SerializeInvariant(this SettingsStorage settings, string fileName)
		{
			if (settings is null)
				throw new ArgumentNullException(nameof(settings));

			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			Do.Invariant(() => settings.Serialize(fileName));
		}

		/// <summary>
		/// Serialize the specified storage into byte array.
		/// </summary>
		/// <param name="settings"><see cref="SettingsStorage"/></param>
		/// <returns></returns>
		public static byte[] SerializeInvariant(this SettingsStorage settings)
			=> Do.Invariant(() => settings.Serialize());

		/// <summary>
		/// Deserialize storage from the specified file.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <returns><see cref="SettingsStorage"/></returns>
		public static SettingsStorage DeserializeInvariant(this string fileName)
			=> Do.Invariant(() => fileName.Deserialize<SettingsStorage>());

		/// <summary>
		/// Deserialize storage from the specified byte array.
		/// </summary>
		/// <param name="data">Data.</param>
		/// <returns><see cref="SettingsStorage"/></returns>
		public static SettingsStorage DeserializeInvariant(this byte[] data)
			=> Do.Invariant(() => data.Deserialize<SettingsStorage>());
	}
}