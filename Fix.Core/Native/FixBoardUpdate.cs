namespace StockSharp.Fix.Native;

/// <summary>
/// Data from BoardUpdate FIX message.
/// </summary>
/// <param name="Board">Board code.</param>
/// <param name="Exchange">Exchange code.</param>
public record struct FixBoardUpdate(
	string Board,
	string Exchange);
