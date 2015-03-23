namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Сообщение об ошибке.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ErrorMessage : Message
	{
		/// <summary>
		/// Создать <see cref="ErrorMessage"/>.
		/// </summary>
		public ErrorMessage()
			: base(MessageTypes.Error)
		{
		}

		/// <summary>
		/// Информация об ошибке.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Error={Message}".PutEx(Error);
		}

		/// <summary>
		/// Создать копию объекта <see cref="ErrorMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new ErrorMessage
			{
				Error = Error,
				LocalTime = LocalTime,
			};
		}
	}
}