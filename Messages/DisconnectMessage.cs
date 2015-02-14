namespace StockSharp.Messages
{
	/// <summary>
	/// Сообщение об отключении от торговой системы (при отправке используется как команда, при получении является событием отключения).
	/// </summary>
	public class DisconnectMessage : BaseConnectionMessage
	{
		/// <summary>
		/// Создать <see cref="DisconnectMessage"/>.
		/// </summary>
		public DisconnectMessage()
			: base(MessageTypes.Disconnect)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="DisconnectMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new DisconnectMessage
			{
				Error = Error,
				LocalTime = LocalTime,
			};
		}
	}
}