namespace StockSharp.Diagram;

/// <summary>
/// Undo/redo manager.
/// </summary>
public interface IUndoManager
{
	/// <summary>
	/// This property is <see langword="true"/> during a call to <see cref="Undo" /> or <see cref="Redo" />.
	/// </summary>
	bool IsUndoingRedoing { get; }

	/// <summary>
	/// Can undo.
	/// </summary>
	/// <returns>Check result.</returns>
	bool CanUndo();

	/// <summary>
	/// Can redo.
	/// </summary>
	/// <returns>Check result.</returns>
	bool CanRedo();

	/// <summary>
	/// Undo.
	/// </summary>
	void Undo();

	/// <summary>
	/// Redo.
	/// </summary>
	void Redo();
}