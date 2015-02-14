namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Описание ошибки, которая произошла при регистрации или отмене заявки.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public class OrderFail : IExtendableEntity
	{
		/// <summary>
		/// Создать <see cref="OrderFail"/>.
		/// </summary>
		public OrderFail()
		{
		}

		/// <summary>
		/// Заявка, которая не была зарегистрирована или отменена из-за ошибки.
		/// </summary>
		[DataMember]
		[RelationSingle]
		public Order Order { get; set; }

		/// <summary>
		/// Системная информация об ошибке, содержащее причину отказа в регистрации или отмене.
		/// </summary>
		[DataMember]
		[BinaryFormatter]
		public Exception Error { get; set; }

		/// <summary>
		/// Серверное время.
		/// </summary>
		[DataMember]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Метка локального времени, когда ошибка была получена.
		/// </summary>
		public DateTime LocalTime { get; set; }

		/// <summary>
		/// Расширенная информация по заявке с ошибкой.
		/// </summary>
		[XmlIgnore]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return Order.ExtensionInfo; }
			set { Order.ExtensionInfo = value; }
		}
	}
}