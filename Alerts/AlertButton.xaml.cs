#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alerts.Alerts
File: AlertButton.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Alerts
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Messages;

	/// <summary>
	/// Button activation alert settings.
	/// </summary>
	public partial class AlertButton : IPersistable, IDisposable
	{
		private AlertSchema _alertSchema;

		/// <summary>
		/// Initializes a new instance of the <see cref="AlertButton"/>.
		/// </summary>
		public AlertButton()
		{
			InitializeComponent();
		}

		private static IAlertService _alertService;

		private static IAlertService AlertService => _alertService ?? (_alertService = ConfigManager.GetService<IAlertService>());

		/// <summary>
		/// Message type.
		/// </summary>
		public Type MessageType
		{
			get { return _alertSchema?.MessageType; }
			set
			{
				_alertSchema = value == null ? null : new AlertSchema(value);
				IsEnabled = value != null;

				if (_alertSchema != null)
					AlertService.Register(_alertSchema);
			}
		}

		/// <summary>
		/// Schema change event.
		/// </summary>
		public event Action SchemaChanged;

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			if (_alertSchema == null)
				return;

			var alertSettings = storage.GetValue<SettingsStorage>(nameof(AlertSchema));
			if (alertSettings != null)
				_alertSchema.Load(alertSettings);

			TryRegisterAlertSchema();
			IsChecked = _alertSchema.AlertType != null;
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			if (_alertSchema == null)
				return;

			storage.SetValue(nameof(AlertSchema), _alertSchema.Save());
		}

		private void AlertButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (_alertSchema == null)
				return;

			if (new AlertSettingsWindow { Schema = _alertSchema }.ShowModal(this))
			{
				TryRegisterAlertSchema();
				IsChecked = _alertSchema.AlertType != null;
				SchemaChanged?.Invoke();
			}
		}

		private void TryRegisterAlertSchema()
		{
			if (_alertSchema.AlertType != null)
				AlertService.Register(_alertSchema);
		}

		/// <summary>
		/// Process activation alert messages.
		/// </summary>
		/// <param name="messages">Messages.</param>
		public void Process(IEnumerable<Message> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			if (_alertSchema?.AlertType == null)
				return;

			foreach (var message in messages)
			{
				AlertService.Process(message);
			}
		}

		/// <summary>
		/// Process activation alert message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (_alertSchema?.AlertType == null)
				return;

			AlertService.Process(message);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			if (_alertSchema == null)
				return;

			AlertService.UnRegister(_alertSchema);
		}
	}
}