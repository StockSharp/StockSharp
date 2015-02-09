namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class BoardsResponse : BaseResponse
	{
		public IEnumerable<Board> Boards { get; internal set; }
	}

	internal class Board
	{
		/// <summary>
		/// Идентификатор режима торгов.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Наименование режима торгов.
		/// </summary>
		public string Name { get; set; }
		
		/// <summary>
		/// Внутренний код рынка.
		/// </summary>
		public int Market { get; set; }
		
		/// <summary>
		/// Тип режима торгов 0=FORTS, 1=Т+, 2= Т0.
		/// </summary>
		public int Type { get; set; }
	}
}
