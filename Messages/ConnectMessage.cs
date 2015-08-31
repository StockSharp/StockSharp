namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Сообщение о подключении к торговой системе (при отправке используется как команда, при получении является событием подключения).
	/// </summary>
	[DataContract]
	[Serializable]
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
		/// Создать копию <see cref="ConnectMessage"/>.
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