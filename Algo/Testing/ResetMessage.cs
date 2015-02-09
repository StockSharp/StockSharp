namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// Сообщение, информирующее о сбросе состояния эмулятора.
	/// </summary>
	public sealed class ResetMessage : Message
	{
		/// <summary>
		/// Создать <see cref="ResetMessage"/>.
		/// </summary>
		public ResetMessage()
			: base(ExtendedMessageTypes.Reset)
		{
		}
	}
}