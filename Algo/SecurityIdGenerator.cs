namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The instrument identifiers generator <see cref="Security.Id"/>.
	/// </summary>
	public class SecurityIdGenerator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityIdGenerator"/>.
		/// </summary>
		public SecurityIdGenerator()
		{
			Delimiter = "@";
		}

		/// <summary>
		/// The delimiter between the instrument code and the class.
		/// </summary>
		public string Delimiter { get; set; }

		/// <summary>
		/// Generate <see cref="Security.Id"/> security.
		/// </summary>
		/// <param name="secCode">Security code.</param>
		/// <param name="boardCode">Board code.</param>
		/// <returns><see cref="Security.Id"/> security.</returns>
		public virtual string GenerateId(string secCode, string boardCode)
		{
			if (secCode.IsEmpty())
				throw new ArgumentNullException("secCode");

			if (boardCode.IsEmpty())
				throw new ArgumentNullException("boardCode");

			return (secCode + Delimiter + boardCode).ToUpperInvariant();
		}

		/// <summary>
		/// Generate <see cref="Security.Id"/> security.
		/// </summary>
		/// <param name="secCode">Security code.</param>
		/// <param name="board">Security boeard.</param>
		/// <returns><see cref="Security.Id"/> security.</returns>
		public virtual string GenerateId(string secCode/*, string secClass*/, ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException("board");

			return GenerateId(secCode, board.Code);
		}

		/// <summary>
		/// To get instrument codes and boards by the instrument identifier.
		/// </summary>
		/// <param name="securityId">The instrument identifier <see cref="Security.Id"/>.</param>
		/// <param name="nullIfInvalid">Return <see langword="null"/> in case of <paramref name="securityId"/> is invalid value.</param>
		/// <returns>The instrument code <see cref="SecurityId.SecurityCode"/> and the board code <see cref="SecurityId.BoardCode"/>.</returns>
		public virtual SecurityId Split(string securityId, bool nullIfInvalid = false)
		{
			if (securityId.IsEmpty())
				throw new ArgumentNullException("securityId");

			var index = securityId.LastIndexOf(Delimiter, StringComparison.InvariantCulture);

			return index == -1
				? nullIfInvalid ? default(SecurityId) : new SecurityId { SecurityCode = securityId, BoardCode = ExchangeBoard.Associated.Code }
				: new SecurityId { SecurityCode = securityId.Substring(0, index), BoardCode = securityId.Substring(index + Delimiter.Length, securityId.Length - index - Delimiter.Length) };
		}
	}
}