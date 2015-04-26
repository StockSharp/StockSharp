namespace StockSharp.Messages
{
	/// <summary>
	/// Сообщение, информирующее о сбросе состояния эмулятора.
	/// </summary>
	public sealed class ResetMessage : Message
	{
		/// <summary>
		/// Создать <see cref="ResetMessage"/>.
		/// </summary>
		public ResetMessage()
			: base(MessageTypes.Reset)
		{
		}
	}
}