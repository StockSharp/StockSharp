namespace StockSharp.Alerts
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Speech.Synthesis;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Community;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Сервис сигналов.
	/// </summary>
	public class AlertService : Disposable, IAlertService
	{
		private readonly BlockingQueue<Tuple<AlertTypes, string, string, DateTime>> _alerts = new BlockingQueue<Tuple<AlertTypes, string, string, DateTime>>();
		private readonly SynchronizedDictionary<Type, AlertSchema> _schemas = new SynchronizedDictionary<Type, AlertSchema>(); 

		/// <summary>
		/// Создать <see cref="AlertService"/>.
		/// </summary>
		/// <param name="dumpDir">Директория, куда сервис будет сохранять временные файлы.</param>
		public AlertService(string dumpDir)
		{
			if (dumpDir.IsEmpty())
				throw new ArgumentNullException("dumpDir");

			ThreadingHelper
				.Thread(() =>
				{
					try
					{
						var player = new MediaPlayer();

						var fileName = Path.Combine(dumpDir, "alert.mp3");

						if (!File.Exists(fileName))
							Properties.Resources.Alert.Save(fileName);

						player.Open(new Uri(fileName, UriKind.RelativeOrAbsolute));

						var logManager = ConfigManager.GetService<LogManager>();

						using (var speech = new SpeechSynthesizer())
						using (var client = new NotificationClient())
						using (player.MakeDisposable(p => p.Close()))
						{
							while (!IsDisposed)
							{
								Tuple<AlertTypes, string, string, DateTime> alert;

								if (!_alerts.TryDequeue(out alert))
									break;

								try
								{
									switch (alert.Item1)
									{
										case AlertTypes.Sound:
											player.Play();
											break;
										case AlertTypes.Speech:
											speech.Speak(alert.Item2);
											break;
										case AlertTypes.Popup:
											GuiDispatcher.GlobalDispatcher.AddAction(() => new AlertPopupWindow
											{
												Title = alert.Item2,
												Message = alert.Item3,
												Time = alert.Item4
											}.Show());
											break;
										case AlertTypes.Sms:
											client.SendSms(alert.Item2);
											break;
										case AlertTypes.Email:
											client.SendEmail(alert.Item2, alert.Item3);
											break;
										case AlertTypes.Log:
											logManager.Application.AddWarningLog(() => LocalizedStrings.Str3033Params
												.Put(alert.Item4, alert.Item2, Environment.NewLine + alert.Item3));
											break;
										default:
											throw new ArgumentOutOfRangeException();
									}
								}
								catch (Exception ex)
								{
									ex.LogError();
								}
							}
						}
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
				})
				.Name("Alert thread")
				.Launch();
		}

		/// <summary>
		/// Добавить сигнал на вывод.
		/// </summary>
		/// <param name="type">Тип сигнала.</param>
		/// <param name="caption">Заголовок сигнала.</param>
		/// <param name="message">Текст сигнала.</param>
		/// <param name="time">Время формирования.</param>
		public void PushAlert(AlertTypes type, string caption, string message, DateTime time)
		{
			_alerts.Enqueue(Tuple.Create(type, caption, message, time));
		}

		/// <summary>
		/// Зарегистрировать схему.
		/// </summary>
		/// <param name="schema">Схема.</param>
		public void Register(AlertSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			_schemas[schema.MessageType] = schema;
		}

		/// <summary>
		/// Удалить ранее зарегистрированную через <see cref="IAlertService.Register"/> схему.
		/// </summary>
		/// <param name="schema">Схема.</param>
		public void UnRegister(AlertSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			_schemas.Remove(schema.MessageType);
		}

		/// <summary>
		/// Проверить сообщение на активацию сигнала.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var schema = _schemas.TryGetValue(message.GetType());

			if (schema == null)
				return;

			var type = schema.AlertType;

			if (type == null)
				return;

			var canAlert = schema.Rules.All(rule =>
			{
				var value = rule.Property.GetValue(message, null);

				switch (rule.Operator)
				{
					case ComparisonOperator.Equal:
						return rule.Value.Equals(value);
					case ComparisonOperator.NotEqual:
						return !rule.Value.Equals(value);
					case ComparisonOperator.Greater:
						return OperatorRegistry.GetOperator(rule.Property.PropertyType).Compare(rule.Value, value) == 1;
					case ComparisonOperator.GreaterOrEqual:
						return OperatorRegistry.GetOperator(rule.Property.PropertyType).Compare(rule.Value, value) >= 0;
					case ComparisonOperator.Less:
						return OperatorRegistry.GetOperator(rule.Property.PropertyType).Compare(rule.Value, value) == -1;
					case ComparisonOperator.LessOrEqual:
						return OperatorRegistry.GetOperator(rule.Property.PropertyType).Compare(rule.Value, value) <= 0;
					case ComparisonOperator.Any:
						return true;
					default:
						throw new ArgumentOutOfRangeException();
				}
			});

			if (canAlert)
				PushAlert((AlertTypes)type, schema.Caption, schema.Message, message.LocalTime);
		}
		
		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_alerts.Close();
			base.DisposeManaged();
		}
	}
}