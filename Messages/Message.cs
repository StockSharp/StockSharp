namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее рыночные данные или команду.
	/// </summary>
	public abstract class Message : Cloneable<Message>, IExtendableEntity
	{
		/// <summary>
		/// Метка локального времени, когда сообщение было получено/создано.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str203Key)]
		[DescriptionLoc(LocalizedStrings.Str204Key)]
		[MainCategory]
		public DateTime LocalTime { get; set; }

		/// <summary>
		/// Тип сообщения.
		/// </summary>
		public MessageTypes Type { get; private set; }

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Расширенная информация.
		/// </summary>
		/// <remarks>
		/// Необходима в случае хранения в программе дополнительной информации, ассоциированной с сообщением.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set { _extensionInfo = value; }
		}

		/// <summary>
		/// Инициализировать <see cref="Message"/>.
		/// </summary>
		/// <param name="type">Тип сообщения.</param>
		protected Message(MessageTypes type)
		{
			Type = type;
		}

		/// <summary>
		/// Добавить задержку ко времени сообщения. Используется в эмуляции.
		/// </summary>
		/// <param name="diff">Значение задержки.</param>
		public void AddLatency(TimeSpan diff)
		{
			LocalTime += diff;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Type + ",T(L)={0:yyyy/MM/dd HH:mm:ss.fff}".Put(LocalTime);
		}

		/// <summary>
		/// Создать копию объекта <see cref="Message"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			throw new NotSupportedException();
		}
	}
}