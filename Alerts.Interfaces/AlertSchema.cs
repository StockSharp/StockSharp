#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alerts.Alerts
File: AlertSchema.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Alerts
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

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
		public AlertSchema()
		{
			Rules = new List<AlertRule>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AlertSchema"/>.
		/// </summary>
		/// <param name="messageType">Message type.</param>
		public AlertSchema(Type messageType)
		{
			MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
			Rules = new List<AlertRule>();
		}

		/// <summary>
		/// Identifier.
		/// </summary>
		public Guid Id { get; private set; } = Guid.NewGuid();

		/// <summary>
		/// Enabled.
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Message type.
		/// </summary>
		public Type MessageType { get; private set; }

		/// <summary>
		/// Rules.
		/// </summary>
		public IList<AlertRule> Rules { get; }

		/// <summary>
		/// Alert type.
		/// </summary>
		public AlertNotifications? AlertType { get; set; }

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
			Rules.Clear();
			Rules.AddRange(storage.GetValue<SettingsStorage[]>(nameof(Rules)).Select(s => s.Load<AlertRule>()).Where(r => r.Value != null));

			AlertType = storage.GetValue<string>(nameof(AlertType)).To<AlertNotifications?>();
			Caption = storage.GetValue<string>(nameof(Caption));
			Message = storage.GetValue<string>(nameof(Message));
			IsEnabled = storage.GetValue(nameof(IsEnabled), IsEnabled);
			Id = storage.GetValue<Guid>(nameof(Id));
			MessageType = storage.GetValue<string>(nameof(MessageType)).To<Type>();
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Rules), Rules.Select(r => r.Save()).ToArray());
			storage.SetValue(nameof(AlertType), AlertType.To<string>());
			storage.SetValue(nameof(Caption), Caption);
			storage.SetValue(nameof(Message), Message);
			storage.SetValue(nameof(IsEnabled), IsEnabled);
			storage.SetValue(nameof(Id), Id);
			storage.SetValue(nameof(MessageType), MessageType.GetTypeName(false));
		}
	}
}