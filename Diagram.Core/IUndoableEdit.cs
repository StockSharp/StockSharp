namespace StockSharp.Diagram;

/// <summary>
/// This interface specifies how a document change (an edit).
/// </summary>
public interface IUndoableEdit
{
	/// <summary>
	/// Forget about any state remembered in this edit.
	/// </summary>
	void Clear();

	/// <summary>
	/// Determine if this edit is ready to be and can be undone.
	/// </summary>
	/// <returns></returns>
	bool CanUndo();

	/// <summary>
	/// Restore the previous state of this edit.
	/// </summary>
	void Undo();

	/// <summary>
	/// Determine if this edit is ready to be and can be redone.
	/// </summary>
	/// <returns></returns>
	bool CanRedo();

	/// <summary>
	/// Restore the new state of this edit after having been undone.
	/// </summary>
	void Redo();
}
