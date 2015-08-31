namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее рыночные данные или команду.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class Message : Cloneable<Message>, IExtendableEntity
	{
		/// <summary>
		/// Метка локального времени, когда сообщение было получено/создано.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str203Key)]
		[DescriptionLoc(LocalizedStrings.Str204Key)]
		[MainCategory]
		[DataMember]
		public DateTime LocalTime { get; set; }

		[field: NonSerialized]
		private readonly MessageTypes _type;

		/// <summary>
		/// Тип сообщения.
		/// </summary>
		public MessageTypes Type
		{
			get { return _type; }
		}

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
		/// Следует ли отправлять сообщение обратно отправителю.
		/// </summary>
		public bool IsBack { get; set; }

		/// <summary>
		/// Адаптер, отправивший сообщение. Может быть <see langword="null"/>.
		/// </summary>
		public IMessageAdapter Adapter { get; set; }

		/// <summary>
		/// Инициализировать <see cref="Message"/>.
		/// </summary>
		/// <param name="type">Тип сообщения.</param>
		protected Message(MessageTypes type)
		{
			_type = type;
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
		/// Создать копию <see cref="Message"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			throw new NotSupportedException();
		}
	}
}