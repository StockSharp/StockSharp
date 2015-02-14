namespace StockSharp.Messages
{
	/// <summary>
	/// Сообщение о подключении к торговой системе (при отправке используется как команда, при получении является событием подключения).
	/// </summary>
	public class ConnectMessage : BaseConnectionMessage
	{
		/// <summary>
		/// Создать <see cref="ConnectMessage"/>.
		/// </summary>
		public ConnectMessage()
			: base(MessageTypes.Connect)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="ConnectMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new ConnectMessage
			{
				Error = Error,
				LocalTime = LocalTime,
			};
		}
	}
}