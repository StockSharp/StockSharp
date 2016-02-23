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
		public Type MessageType { get; }

		/// <summary>
		/// Rules.
		/// </summary>
		public IList<AlertRule> Rules { get; }

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
			Rules.AddRange(storage.GetValue<IList<AlertRule>>(nameof(Rules)));
			AlertType = storage.GetValue<string>(nameof(AlertType)).To<AlertTypes?>();
			Caption = storage.GetValue<string>(nameof(Caption));
			Message = storage.GetValue<string>(nameof(Message));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Rules), Rules);
			storage.SetValue(nameof(AlertType), AlertType.To<string>());
			storage.SetValue(nameof(Caption), Caption);
			storage.SetValue(nameof(Message), Message);
		}
	}
}