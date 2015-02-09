namespace StockSharp.Alerts
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Messages;

	/// <summary>
	/// Кнопка активации настроек сигнала.
	/// </summary>
	public partial class AlertButton : IPersistable, IDisposable
	{
		private AlertSchema _alertSchema;

		/// <summary>
		/// Создать <see cref="AlertButton"/>.
		/// </summary>
		public AlertButton()
		{
			InitializeComponent();
		}

		private static IAlertService _alertService;

		private static IAlertService AlertService
		{
			get { return _alertService ?? (_alertService = ConfigManager.GetService<IAlertService>()); }
		}

		/// <summary>
		/// Тип сообщения.
		/// </summary>
		public Type MessageType
		{
			get { return _alertSchema == null ? null : _alertSchema.MessageType; }
			set
			{
				_alertSchema = value == null ? null : new AlertSchema(value);
				IsEnabled = value != null;
			}
		}

		/// <summary>
		/// Событие изменения схемы.
		/// </summary>
		public event Action SchemaChanged;

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			if (_alertSchema == null)
				return;

			var alertSettings = storage.GetValue<SettingsStorage>("AlertSchema");
			if (alertSettings != null)
				_alertSchema.Load(alertSettings);

			TryRegisterAlertSchema();
			IsChecked = _alertSchema.AlertType != null;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			if (_alertSchema == null)
				return;

			storage.SetValue("AlertSchema", _alertSchema.Save());
		}

		private void AlertButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (_alertSchema == null)
				return;

			if (new AlertSettingsWindow { Schema = _alertSchema }.ShowModal(this))
			{
				TryRegisterAlertSchema();
				IsChecked = _alertSchema.AlertType != null;
				SchemaChanged.SafeInvoke();
			}
		}

		private void TryRegisterAlertSchema()
		{
			if (_alertSchema.AlertType != null)
				AlertService.Register(_alertSchema);
		}

		/// <summary>
		/// Обработать сообщения на активацию сигнала.
		/// </summary>
		/// <param name="messages">Сообщения.</param>
		public void Process(IEnumerable<Message> messages)
		{
			if (messages == null)
				throw new ArgumentNullException("messages");

			if (_alertSchema == null)
				return;

			if (_alertSchema.AlertType == null)
				return;

			foreach (var message in messages)
			{
				AlertService.Process(message);
			}
		}

		/// <summary>
		/// Обработать сообщение на активацию сигнала.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (_alertSchema == null)
				return;

			if (_alertSchema.AlertType == null)
				return;

			AlertService.Process(message);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		public void Dispose()
		{
			if (_alertSchema == null)
				return;

			AlertService.UnRegister(_alertSchema);
		}
	}
}