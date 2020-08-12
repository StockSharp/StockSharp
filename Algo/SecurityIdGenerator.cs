#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: SecurityIdGenerator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
				throw new ArgumentNullException(nameof(secCode));

			if (boardCode.IsEmpty())
				throw new ArgumentNullException(nameof(boardCode));

			return (secCode + Delimiter + boardCode).ToUpperInvariant();
		}

		/// <summary>
		/// Generate <see cref="Security.Id"/> security.
		/// </summary>
		/// <param name="secCode">Security code.</param>
		/// <param name="board">Security board.</param>
		/// <returns><see cref="Security.Id"/> security.</returns>
		public virtual string GenerateId(string secCode/*, string secClass*/, ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

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
				throw new ArgumentNullException(nameof(securityId));

			var index = securityId.LastIndexOfIgnoreCase(Delimiter);

			return index == -1
				? nullIfInvalid ? default : new SecurityId { SecurityCode = securityId, BoardCode = ExchangeBoard.Associated.Code }
				: new SecurityId { SecurityCode = securityId.Substring(0, index), BoardCode = securityId.Substring(index + Delimiter.Length, securityId.Length - index - Delimiter.Length) };
		}
	}
}