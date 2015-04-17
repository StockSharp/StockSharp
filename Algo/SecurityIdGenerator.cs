namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Генератор идентификаторов инструментов <see cref="Security.Id"/>.
	/// </summary>
	public class SecurityIdGenerator
	{
		/// <summary>
		/// Создать <see cref="SecurityIdGenerator"/>.
		/// </summary>
		public SecurityIdGenerator()
		{
			Delimiter = "@";
		}

		/// <summary>
		/// Разделитель между кодом и классом инструмента.
		/// </summary>
		public string Delimiter { get; set; }

		/// <summary>
		/// Сгенерировать <see cref="Security.Id"/> инструмента.
		/// </summary>
		/// <param name="secCode">Код инструмента.</param>
		/// <param name="boardCode">Код площадки.</param>
		/// <returns><see cref="Security.Id"/> инструмента.</returns>
		public virtual string GenerateId(string secCode, string boardCode)
		{
			if (secCode.IsEmpty())
				throw new ArgumentNullException("secCode");

			if (boardCode.IsEmpty())
				throw new ArgumentNullException("boardCode");

			return (secCode + Delimiter + boardCode).ToUpperInvariant();
		}

		/// <summary>
		/// Сгенерировать <see cref="Security.Id"/> инструмента.
		/// </summary>
		/// <param name="secCode">Код инструмента.</param>
		/// <param name="board">Площадка инструмента.</param>
		/// <returns><see cref="Security.Id"/> инструмента.</returns>
		public virtual string GenerateId(string secCode/*, string secClass*/, ExchangeBoard board)
		{
			if (secCode.IsEmpty())
				throw new ArgumentNullException("secCode");

			if (board == null)
				throw new ArgumentNullException("board");

			return GenerateId(secCode, board.Code);
		}

		/// <summary>
        /// Get codes tools and platforms on the identifier of the tool.
		/// </summary>
		/// <param name="securityId">Tool ID <see cref="Security.Id"/>.</param>
		/// <returns>Get Quotes <see cref="Security.Code"/> and code pad <see cref="Security.Board"/>.</returns>
        public virtual Tuple<string, string> Split(string securityId)
        {
            if (securityId.IsEmpty())
                throw new ArgumentNullException("securityId");

            var index = securityId.LastIndexOf(Delimiter, StringComparison.InvariantCulture);

            return index == -1
             ? Tuple.Create(securityId, ExchangeBoard.Associated.Code)
             : Tuple.Create(securityId.Substring(0, index), securityId.Substring(index + Delimiter.Length, securityId.Length - index - Delimiter.Length));
        }
	}
}