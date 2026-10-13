namespace StockSharp.Configuration;

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
	[Obsolete("Blocking sync-over-async wrapper. Use SerializeInvariantAsync instead.")]
	public static void SerializeInvariant(this SettingsStorage settings, IFileSystem fileSystem, string fileName, bool bom = true)
		=> AsyncHelper.Run(() => settings.SerializeInvariantAsync(fileSystem, fileName, bom, default));

	/// <summary>
	/// Serialize the specified storage into file.
	/// </summary>
	/// <param name="settings"><see cref="SettingsStorage"/></param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="fileName">File name.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public static async ValueTask SerializeInvariantAsync(this SettingsStorage settings, IFileSystem fileSystem, string fileName, bool bom, CancellationToken cancellationToken)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		var bytes = await settings.SerializeInvariantAsync(bom, cancellationToken);

		cancellationToken.ThrowIfCancellationRequested();

		// Opening the file empties it, so the write that follows is not cancelled: it would leave the file empty.
		await using var stream = fileSystem.OpenWrite(fileName);
		await stream.WriteAsync(bytes, CancellationToken.None);
	}

	/// <summary>
	/// Deserialize storage from the specified file using <see cref="IFileSystem"/>.
	/// </summary>
	/// <param name="fileSystem">File system.</param>
	/// <param name="fileName">File name.</param>
	/// <returns><see cref="SettingsStorage"/></returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeInvariantAsync instead.")]
	public static SettingsStorage DeserializeInvariant(this IFileSystem fileSystem, string fileName)
		=> AsyncHelper.Run(() => fileSystem.DeserializeInvariantAsync(fileName, default));

	/// <summary>
	/// Deserialize storage from the specified file using <see cref="IFileSystem"/>.
	/// </summary>
	/// <param name="fileSystem">File system.</param>
	/// <param name="fileName">File name.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="SettingsStorage"/></returns>
	public static async ValueTask<SettingsStorage> DeserializeInvariantAsync(this IFileSystem fileSystem, string fileName, CancellationToken cancellationToken)
	{
		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		using var stream = fileSystem.OpenRead(fileName);
		using var ms = new MemoryStream();
		await stream.CopyToAsync(ms, cancellationToken);

		return await ms.ToArray().DeserializeInvariantAsync(cancellationToken);
	}

	/// <summary>
	/// Serialize the specified storage into file.
	/// </summary>
	/// <param name="settings"><see cref="SettingsStorage"/></param>
	/// <param name="fileName">File name.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	[Obsolete("Use IFileSystem overload.")]
	public static void SerializeInvariant(this SettingsStorage settings, string fileName, bool bom = true)
		=> SerializeInvariant(settings, Paths.FileSystem, fileName, bom);

	/// <summary>
	/// Serialize the specified storage into byte array.
	/// </summary>
	/// <param name="settings"><see cref="IAsyncPersistable"/></param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <returns></returns>
	[Obsolete("Blocking sync-over-async wrapper. Use SerializeInvariantAsync instead.")]
	public static byte[] SerializeInvariant(this IAsyncPersistable settings, bool bom = true)
		=> AsyncHelper.Run(() => settings.SerializeInvariantAsync(bom, default));

	/// <summary>
	/// Serialize the specified settings into byte array.
	/// </summary>
	/// <param name="settings"><see cref="IAsyncPersistable"/></param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Serialized data.</returns>
	public static async ValueTask<byte[]> SerializeInvariantAsync(this IAsyncPersistable settings, bool bom, CancellationToken cancellationToken)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		var storage = await settings.SaveAsync(cancellationToken);

		return await storage.SerializeInvariantAsync(bom, cancellationToken);
	}

	/// <summary>
	/// Serialize the specified storage into byte array.
	/// </summary>
	/// <param name="settings"><see cref="SettingsStorage"/></param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <returns></returns>
	[Obsolete("Blocking sync-over-async wrapper. Use SerializeInvariantAsync instead.")]
	public static byte[] SerializeInvariant(this SettingsStorage settings, bool bom = true)
		=> AsyncHelper.Run(() => settings.SerializeInvariantAsync(bom, default));

	/// <summary>
	/// Serialize the specified storage into byte array.
	/// </summary>
	/// <param name="settings"><see cref="SettingsStorage"/></param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Serialized data.</returns>
	public static async ValueTask<byte[]> SerializeInvariantAsync(this SettingsStorage settings, bool bom, CancellationToken cancellationToken)
		=> await Do.InvariantAsync(async () => await settings.SerializeAsync(bom, cancellationToken));

	/// <summary>
	/// Deserialize storage from the specified file.
	/// </summary>
	/// <param name="fileName">File name.</param>
	/// <returns><see cref="SettingsStorage"/></returns>
	[Obsolete("Use IFileSystem overload.")]
	public static SettingsStorage DeserializeInvariant(this string fileName)
		=> DeserializeInvariant(fileName, Paths.FileSystem);

	/// <summary>
	/// Deserialize storage from the specified file.
	/// </summary>
	/// <param name="fileName">File name.</param>
	/// <param name="fileSystem">File system.</param>
	/// <returns><see cref="SettingsStorage"/></returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeInvariantAsync instead.")]
	public static SettingsStorage DeserializeInvariant(this string fileName, IFileSystem fileSystem)
		=> AsyncHelper.Run(() => fileName.DeserializeInvariantAsync<SettingsStorage>(fileSystem, default));

	/// <summary>
	/// Deserialize storage from the specified file.
	/// </summary>
	/// <param name="fileName">File name.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="SettingsStorage"/></returns>
	public static ValueTask<SettingsStorage> DeserializeInvariantAsync(this string fileName, IFileSystem fileSystem, CancellationToken cancellationToken)
		=> fileName.DeserializeInvariantAsync<SettingsStorage>(fileSystem, cancellationToken);

	/// <summary>
	/// Deserialize storage from the specified file.
	/// </summary>
	/// <typeparam name="T">Type implemented <see cref="IAsyncPersistable"/>.</typeparam>
	/// <param name="fileName">File name.</param>
	/// <param name="fileSystem">File system.</param>
	/// <returns><typeparamref name="T"/></returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeInvariantAsync instead.")]
	public static T DeserializeInvariant<T>(this string fileName, IFileSystem fileSystem)
		=> AsyncHelper.Run(() => fileName.DeserializeInvariantAsync<T>(fileSystem, default));

	/// <summary>
	/// Deserialize storage from the specified file.
	/// </summary>
	/// <typeparam name="T">Type implemented <see cref="IAsyncPersistable"/>.</typeparam>
	/// <param name="fileName">File name.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><typeparamref name="T"/></returns>
	public static async ValueTask<T> DeserializeInvariantAsync<T>(this string fileName, IFileSystem fileSystem, CancellationToken cancellationToken)
		=> await Do.InvariantAsync(() => fileName.DeserializeAsync<T>(fileSystem, cancellationToken).AsTask());

	/// <summary>
	/// Deserialize storage from the specified byte array.
	/// </summary>
	/// <param name="data">Data.</param>
	/// <returns><see cref="SettingsStorage"/></returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeInvariantAsync instead.")]
	public static SettingsStorage DeserializeInvariant(this byte[] data)
		=> AsyncHelper.Run(() => data.DeserializeInvariantAsync(default));

	/// <summary>
	/// Deserialize storage from the specified byte array.
	/// </summary>
	/// <param name="data">Data.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="SettingsStorage"/></returns>
	public static async ValueTask<SettingsStorage> DeserializeInvariantAsync(this byte[] data, CancellationToken cancellationToken)
		=> await Do.InvariantAsync(() => data.DeserializeAsync<SettingsStorage>(cancellationToken).AsTask());
}