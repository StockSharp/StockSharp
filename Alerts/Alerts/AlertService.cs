namespace StockSharp.Studio.Services.Alerts
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
	using Ecng.Xaml;

	using StockSharp.Algo.Notification;
	using StockSharp.Logging;
	using StockSharp.Studio.Core;

	class AlertService : Disposable, IAlertService
	{
		private readonly BlockingQueue<Tuple<AlertTypes, string, string, DateTime>> _alerts = new BlockingQueue<Tuple<AlertTypes, string, string, DateTime>>();
		private readonly SynchronizedDictionary<Type, AlertSchema> _schemas = new SynchronizedDictionary<Type, AlertSchema>(); 

		public AlertService()
		{
			ThreadingHelper
				.Thread(() =>
				{
					try
					{
						var player = new MediaPlayer();

						var fileName = Path.Combine(UserConfig.Instance.MainFolder, "alert.mp3");

						if (!File.Exists(fileName))
							File.WriteAllBytes(fileName, Properties.Resources.Alert);

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
											logManager.Application.AddWarningLog(() => "Оповещение! В {0} случилось '{1}'.{2}"
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

		public void PushAlert(AlertTypes type, string caption, string message, DateTime time)
		{
			_alerts.Enqueue(new Tuple<AlertTypes, string, string, DateTime>(type, caption, message, time));
		}

		void IAlertService.Register(AlertSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			_schemas[schema.EntityType] = schema;
		}

		void IAlertService.UnRegister(AlertSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			_schemas.Remove(schema.EntityType);
		}

		void IAlertService.Validate(object entity, DateTime time)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			var schema = _schemas.TryGetValue(entity.GetType());

			if (schema == null)
				return;

			var type = schema.AlertType;

			if (type == null)
				return;

			var canAlert = schema.Rules.All(rule =>
			{
				var value = rule.Property.GetValue(entity, null);

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
				PushAlert((AlertTypes)type, schema.Caption, schema.Message, time);
		}

		protected override void DisposeManaged()
		{
			_alerts.Close();
			base.DisposeManaged();
		}
	}
}