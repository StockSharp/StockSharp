#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: MoveOrderMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	internal class MoveOrderMessage : BaseCommandMessage
	{
		public MoveOrderMessage() : base(ApiCommands.MoveOrder)
		{
		}

		public long TransactionId { get; set; }
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public MoveOrderFlag MoveFlag { get; set; }
	}

	internal enum MoveOrderFlag
	{
		DontChangeQuantity = 0,
		ChangeQuantity,
		IfNotEqualRemoveOrder
	}
}