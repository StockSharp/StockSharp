namespace StockSharp.Configuration;

using System.IO;

using Ecng.Common;

/// <summary>
/// Invariant culture <see cref="ISerializer"/>.
/// </summary>
public static class InvariantCultureSerializer
{
	/// <summary>
	/// Serialize the specified storage into file using <see cref="IFileSystem"/>.
	/// </summary>
	/// <param name="settings"><see cref="SettingsStorage"/></param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="fileName">File name.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	public static void SerializeInvariant(this SettingsStorage settings, IFileSystem fileSystem, string fileName, bool bom = true)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		var bytes = settings.SerializeInvariant(bom);

		using var stream = fileSystem.OpenWrite(fileName);
		stream.Write(bytes, 0, bytes.Length);
	}

	/// <summary>
	/// Deserialize storage from the specified file using <see cref="IFileSystem"/>.
	/// </summary>
	/// <param name="fileSystem">File system.</param>
	/// <param name="fileName">File name.</param>
	/// <returns><see cref="SettingsStorage"/></returns>
	public static SettingsStorage DeserializeInvariant(this IFileSystem fileSystem, string fileName)
	{
		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		using var stream = fileSystem.OpenRead(fileName);
		using var ms = new MemoryStream();
		stream.CopyTo(ms);

		return ms.ToArray().DeserializeInvariant();
	}

	/// <summary>
	/// Serialize the specified storage into file.
	/// </summary>
	/// <param name="settings"><see cref="SettingsStorage"/></param>
	/// <param name="fileName">File name.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	public static void SerializeInvariant(this SettingsStorage settings, string fileName, bool bom = true)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		Do.Invariant(() => settings.Serialize(fileName, bom));
	}

	/// <summary>
	/// Serialize the specified storage into byte array.
	/// </summary>
	/// <param name="settings"><see cref="IPersistable"/></param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <returns></returns>
	public static byte[] SerializeInvariant(this IPersistable settings, bool bom = true)
		=> settings.Save().SerializeInvariant(bom);

	/// <summary>
	/// Serialize the specified storage into byte array.
	/// </summary>
	/// <param name="settings"><see cref="SettingsStorage"/></param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <returns></returns>
	public static byte[] SerializeInvariant(this SettingsStorage settings, bool bom = true)
		=> Do.Invariant(() => settings.Serialize(bom));

	/// <summary>
	/// Deserialize storage from the specified file.
	/// </summary>
	/// <param name="fileName">File name.</param>
	/// <returns><see cref="SettingsStorage"/></returns>
	public static SettingsStorage DeserializeInvariant(this string fileName)
		=> DeserializeInvariant<SettingsStorage>(fileName);

	/// <summary>
	/// Deserialize storage from the specified file.
	/// </summary>
	/// <typeparam name="T">Type implemented <see cref="IPersistable"/>.</typeparam>
	/// <param name="fileName">File name.</param>
	/// <returns><typeparamref name="T"/></returns>
	public static T DeserializeInvariant<T>(this string fileName)
		=> Do.Invariant(fileName.Deserialize<T>);

	/// <summary>
	/// Deserialize storage from the specified byte array.
	/// </summary>
	/// <param name="data">Data.</param>
	/// <returns><see cref="SettingsStorage"/></returns>
	public static SettingsStorage DeserializeInvariant(this byte[] data)
		=> Do.Invariant(data.Deserialize<SettingsStorage>);
}