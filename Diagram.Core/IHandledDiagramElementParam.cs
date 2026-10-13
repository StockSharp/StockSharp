namespace StockSharp.Diagram;

/// <summary>
/// A parameter whose value is persisted by its own save/load handlers.
/// </summary>
internal interface IHandledDiagramElementParam
{
	/// <summary>
	/// The value is persisted by handlers rather than as a persistable object.
	/// </summary>
	bool HasPersistenceHandlers { get; }

	/// <summary>
	/// The handlers are synchronous, so the value can be copied through them.
	/// </summary>
	bool HasSyncHandlers { get; }

	/// <summary>
	/// Saves the value with the synchronous save handler.
	/// </summary>
	/// <returns>The saved value.</returns>
	SettingsStorage SaveByHandler();

	/// <summary>
	/// Loads the value with the synchronous load handler.
	/// </summary>
	/// <param name="storage">The saved value.</param>
	void LoadByHandler(SettingsStorage storage);
}
