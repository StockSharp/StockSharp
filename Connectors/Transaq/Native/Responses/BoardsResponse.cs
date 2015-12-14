#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: BoardsResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
