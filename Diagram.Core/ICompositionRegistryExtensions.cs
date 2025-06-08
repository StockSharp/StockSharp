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
	public static SettingsStorage Serialize(this ICompositionRegistry registry, CompositionDiagramElement element, bool includeCoordinates = true, SecureString password = default)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		if (element is null)
			throw new ArgumentNullException(nameof(element));

		var storage = new SettingsStorage();
		registry.Serialize(element, storage, includeCoordinates, password);
		return storage;
	}

	/// <summary>
	/// To deserialize the composite element.
	/// </summary>
	/// <param name="registry"><see cref="ICompositionRegistry"/></param>
	/// <param name="storage">Settings storage.</param>
	/// <param name="getPassword">Get password handler.</param>
	/// <returns><see cref="CompositionDiagramElement"/></returns>
	public static (CompositionDiagramElement element, bool isEncrypted) Deserialize(this ICompositionRegistry registry, SettingsStorage storage, Func<SecureString> getPassword)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		var element = registry.CreateComposition();
		var isEncrypted = registry.Deserialize(element, storage, getPassword);
		return (element, isEncrypted);
	}
}
