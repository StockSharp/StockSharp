namespace StockSharp.Alerts
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Схема.
	/// </summary>
	public class AlertSchema : IPersistable
	{
		/// <summary>
		/// Создать <see cref="AlertSchema"/>.
		/// </summary>
		/// <param name="messageType">Тип сообщения.</param>
		public AlertSchema(Type messageType)
		{
			MessageType = messageType;
			Rules = new List<AlertRule>();
		}

		/// <summary>
		/// Тип сообщения.
		/// </summary>
		public Type MessageType { get; private set; }

		/// <summary>
		/// Правила.
		/// </summary>
		public IList<AlertRule> Rules { get; private set; }

		/// <summary>
		/// Тип сигнала.
		/// </summary>
		public AlertTypes? AlertType { get; set; }

		/// <summary>
		/// Заголовок сигнала.
		/// </summary>
		public string Caption { get; set; }

		/// <summary>
		/// Текст сигнала.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			Rules.AddRange(storage.GetValue<IList<AlertRule>>("Rules"));
			AlertType = storage.GetValue<string>("AlertType").To<AlertTypes?>();
			Caption = storage.GetValue<string>("Caption");
			Message = storage.GetValue<string>("Message");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Rules", Rules);
			storage.SetValue("AlertType", AlertType.To<string>());
			storage.SetValue("Caption", Caption);
			storage.SetValue("Message", Message);
		}
	}
}