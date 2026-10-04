namespace StockSharp.Diagram;

using System.Security;

/// <summary>
/// <see cref="ICompositionRegistry"/> extension methods.
/// </summary>
public static class ICompositionRegistryExtensions
{
	/// <summary>
	/// Not supported password handler.
	/// </summary>
	public static Func<SecureString> NotSupported { get; } = () => throw new NotSupportedException();

	/// <summary>
	/// To serialize the composite element.
	/// </summary>
	/// <param name="registry"><see cref="ICompositionRegistry"/></param>
	/// <param name="element"><see cref="CompositionDiagramElement"/></param>
	/// <param name="includeCoordinates">Include coordinates.</param>
	/// <param name="password">Password.</param>
	/// <returns>Settings storage.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use SerializeAsync instead.")]
	public static SettingsStorage Serialize(this ICompositionRegistry registry, CompositionDiagramElement element, bool includeCoordinates = true, SecureString password = default)
		=> AsyncHelper.Run(() => registry.SerializeAsync(element, includeCoordinates, password, default));

	/// <summary>
	/// To serialize the composite element.
	/// </summary>
	/// <param name="registry"><see cref="ICompositionRegistry"/></param>
	/// <param name="element"><see cref="CompositionDiagramElement"/></param>
	/// <param name="storage">Settings storage.</param>
	/// <param name="includeCoordinates">Include coordinates.</param>
	/// <param name="password">Password.</param>
	[Obsolete("Blocking sync-over-async wrapper. Use SerializeAsync instead.")]
	public static void Serialize(this ICompositionRegistry registry, CompositionDiagramElement element, SettingsStorage storage, bool includeCoordinates, SecureString password)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		AsyncHelper.Run(() => registry.SerializeAsync(element, storage, includeCoordinates, password, default));
	}

	/// <summary>
	/// To serialize the composite element.
	/// </summary>
	/// <param name="registry"><see cref="ICompositionRegistry"/></param>
	/// <param name="element"><see cref="CompositionDiagramElement"/></param>
	/// <param name="includeCoordinates">Include coordinates.</param>
	/// <param name="password">Password.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Settings storage.</returns>
	public static async ValueTask<SettingsStorage> SerializeAsync(this ICompositionRegistry registry, CompositionDiagramElement element, bool includeCoordinates, SecureString password, CancellationToken cancellationToken)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		if (element is null)
			throw new ArgumentNullException(nameof(element));

		var storage = new SettingsStorage();
		await registry.SerializeAsync(element, storage, includeCoordinates, password, cancellationToken);
		return storage;
	}

	/// <summary>
	/// To deserialize the composite element.
	/// </summary>
	/// <param name="registry"><see cref="ICompositionRegistry"/></param>
	/// <param name="element"><see cref="CompositionDiagramElement"/></param>
	/// <param name="storage">Settings storage.</param>
	/// <param name="getPassword">Get password handler.</param>
	/// <returns>Is encryption used.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeAsync instead.")]
	public static bool Deserialize(this ICompositionRegistry registry, CompositionDiagramElement element, SettingsStorage storage, Func<SecureString> getPassword)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		return AsyncHelper.Run(() => registry.DeserializeAsync(element, storage, getPassword, default));
	}

	/// <summary>
	/// To deserialize the composite element.
	/// </summary>
	/// <param name="registry"><see cref="ICompositionRegistry"/></param>
	/// <param name="storage">Settings storage.</param>
	/// <param name="getPassword">Get password handler.</param>
	/// <returns><see cref="CompositionDiagramElement"/></returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeAsync instead.")]
	public static (CompositionDiagramElement element, bool isEncrypted) Deserialize(this ICompositionRegistry registry, SettingsStorage storage, Func<SecureString> getPassword)
		=> AsyncHelper.Run(() => registry.DeserializeAsync(storage, getPassword, default));

	/// <summary>
	/// To deserialize the composite element.
	/// </summary>
	/// <param name="registry"><see cref="ICompositionRegistry"/></param>
	/// <param name="storage">Settings storage.</param>
	/// <param name="getPassword">Get password handler.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="CompositionDiagramElement"/></returns>
	public static async ValueTask<(CompositionDiagramElement element, bool isEncrypted)> DeserializeAsync(this ICompositionRegistry registry, SettingsStorage storage, Func<SecureString> getPassword, CancellationToken cancellationToken)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		var element = registry.CreateComposition();
		var isEncrypted = await registry.DeserializeAsync(element, storage, getPassword, cancellationToken);
		return (element, isEncrypted);
	}
}
