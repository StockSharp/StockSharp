namespace StockSharp.Algo
{
	using StockSharp.Messages;

	/// <summary>
	/// Сообщение, получаемое от <see cref="BasketMessageAdapter"/>.
	/// </summary>
	public class BasketMessage : Message
	{
		/// <summary>
		/// Сообщение.
		/// </summary>
		public Message Message { get; private set; }

		/// <summary>
		/// Адаптер, который отправил сообщение.
		/// </summary>
		public IMessageAdapter Adapter { get; private set; }

		/// <summary>
		/// Создать <see cref="BasketMessage"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="adapter">Адаптер, который отправил сообщение.</param>
		public BasketMessage(Message message, IMessageAdapter adapter)
			: base(ExtendedMessageTypes.Adapter)
		{
			Message = message;
			Adapter = adapter;
		}
	}
}