namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Базовое сообщение подключения или отключения.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class BaseConnectionMessage : Message
	{
		/// <summary>
		/// Инициализировать <see cref="BaseConnectionMessage"/>.
		/// </summary>
		/// <param name="type">Тип сообщения.</param>
		protected BaseConnectionMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Информация об ошибке. Сигнализирует об ошибке подключения или отключения.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + (Error == null ? null : ",Error={Message}".PutEx(Error));
		}
	}
}