namespace StockSharp.Messages;

/// <summary>
/// The instrument identifiers generator <see cref="SecurityId"/>.
/// </summary>
public class SecurityIdGenerator
{
	private string _delimiter = "@";

	/// <summary>
	/// The delimiter between the instrument code and the class.
	/// </summary>
	public string Delimiter
	{
		get => _delimiter;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_delimiter = value;
		}
	}

	/// <summary>
	/// Generate <see cref="SecurityId"/> security.
	/// </summary>
	/// <param name="secCode">Security code.</param>
	/// <param name="boardCode">Board code.</param>
	/// <returns><see cref="SecurityId"/> security.</returns>
	public virtual string GenerateId(string secCode, string boardCode)
	{
		if (secCode.IsEmpty())
			throw new ArgumentNullException(nameof(secCode));

		if (boardCode.IsEmpty())
			throw new ArgumentNullException(nameof(boardCode));

		return (secCode + Delimiter + boardCode).ToUpperInvariant();
	}

	/// <summary>
	/// To get instrument codes and boards by the instrument identifier.
	/// </summary>
	/// <param name="securityId">The instrument identifier <see cref="SecurityId"/>.</param>
	/// <param name="nullIfInvalid">Return <see langword="null"/> in case of <paramref name="securityId"/> is invalid value.</param>
	/// <returns>The instrument code <see cref="SecurityId.SecurityCode"/> and the board code <see cref="SecurityId.BoardCode"/>.</returns>
	public virtual SecurityId Split(string securityId, bool nullIfInvalid = false)
	{
		if (securityId.IsEmpty())
			throw new ArgumentNullException(nameof(securityId));

		var index = securityId.LastIndexOfIgnoreCase(Delimiter);

		if (index == -1)
			return nullIfInvalid ? default : new SecurityId { SecurityCode = securityId, BoardCode = SecurityId.AssociatedBoardCode };

		var boardCodeStartIndex = index + Delimiter.Length;
		if (boardCodeStartIndex >= securityId.Length)
		{
			// Delimiter is at the end of string, no board code
			return nullIfInvalid ? default : throw new ArgumentException($"Invalid security ID format: {securityId}. Delimiter is at the end.", nameof(securityId));
		}

		var boardCodeLength = securityId.Length - boardCodeStartIndex;
		if (boardCodeLength <= 0)
		{
			// Invalid board code length
			return nullIfInvalid ? default : throw new ArgumentException($"Invalid security ID format: {securityId}. Invalid board code.", nameof(securityId));
		}

		return new SecurityId
		{
			SecurityCode = securityId.Substring(0, index),
			BoardCode = securityId.Substring(boardCodeStartIndex, boardCodeLength)
		};
	}
}