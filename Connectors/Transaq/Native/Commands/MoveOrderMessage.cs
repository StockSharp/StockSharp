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