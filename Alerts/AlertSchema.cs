namespace StockSharp.Alerts
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Schema.
	/// </summary>
	public class AlertSchema : IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AlertSchema"/>.
		/// </summary>
		/// <param name="messageType">Message type.</param>
		public AlertSchema(Type messageType)
		{
			MessageType = messageType;
			Rules = new List<AlertRule>();
		}

		/// <summary>
		/// Message type.
		/// </summary>
		public Type MessageType { get; private set; }

		/// <summary>
		/// Rules.
		/// </summary>
		public IList<AlertRule> Rules { get; private set; }

		/// <summary>
		/// Alert type.
		/// </summary>
		public AlertTypes? AlertType { get; set; }

		/// <summary>
		/// Signal header.
		/// </summary>
		public string Caption { get; set; }

		/// <summary>
		/// Alert text.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Rules.AddRange(storage.GetValue<IList<AlertRule>>("Rules"));
			AlertType = storage.GetValue<string>("AlertType").To<AlertTypes?>();
			Caption = storage.GetValue<string>("Caption");
			Message = storage.GetValue<string>("Message");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Rules", Rules);
			storage.SetValue("AlertType", AlertType.To<string>());
			storage.SetValue("Caption", Caption);
			storage.SetValue("Message", Message);
		}
	}
}