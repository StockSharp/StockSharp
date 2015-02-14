namespace StockSharp.Transaq
{
	using StockSharp.Messages;

	class InitMessage : Message
	{
		public const MessageTypes MsgType = (MessageTypes)(-1);

		public InitMessage()
			: base(MsgType)
		{
		}
	}
}