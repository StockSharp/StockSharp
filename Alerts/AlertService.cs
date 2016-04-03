#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alerts.Alerts
File: AlertService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// Alert service.
	/// </summary>
	public class AlertService : Disposable, IAlertService
	{
		private readonly BlockingQueue<Tuple<AlertTypes, string, string, DateTimeOffset>> _alerts = new BlockingQueue<Tuple<AlertTypes, string, string, DateTimeOffset>>();
		private readonly SynchronizedDictionary<Type, AlertSchema> _schemas = new SynchronizedDictionary<Type, AlertSchema>(); 

		/// <summary>
		/// Initializes a new instance of the <see cref="AlertService"/>.
		/// </summary>
		/// <param name="dumpDir">Temp files directory.</param>
		public AlertService(string dumpDir)
		{
			if (dumpDir.IsEmpty())
				throw new ArgumentNullException(nameof(dumpDir));

			ThreadingHelper
				.Thread(() =>
				{
					try
					{
						var player = new MediaPlayer();

						var fileName = Path.Combine(dumpDir, "alert.mp3");

						if (!File.Exists(fileName))
						{
							Directory.CreateDirectory(dumpDir);
							Properties.Resources.Alert.Save(fileName);
						}

						player.Open(new Uri(fileName, UriKind.RelativeOrAbsolute));

						var logManager = ConfigManager.GetService<LogManager>();

						using (var speech = new SpeechSynthesizer())
						using (var client = new NotificationClient())
						using (player.MakeDisposable(p => p.Close()))
						{
							while (!IsDisposed)
							{
								Tuple<AlertTypes, string, string, DateTimeOffset> alert;

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
												Time = alert.Item4.UtcDateTime
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
		/// Add alert at the output.
		/// </summary>
		/// <param name="type">Alert type.</param>
		/// <param name="caption">Signal header.</param>
		/// <param name="message">Alert text.</param>
		/// <param name="time">Creation time.</param>
		public void PushAlert(AlertTypes type, string caption, string message, DateTimeOffset time)
		{
			_alerts.Enqueue(Tuple.Create(type, caption, message, time));
		}

		/// <summary>
		/// Register schema.
		/// </summary>
		/// <param name="schema">Schema.</param>
		public void Register(AlertSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException(nameof(schema));

			_schemas[schema.MessageType] = schema;
		}

		/// <summary>
		/// Remove previously registered by <see cref="Register"/> schema.
		/// </summary>
		/// <param name="schema">Schema.</param>
		public void UnRegister(AlertSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException(nameof(schema));

			_schemas.Remove(schema.MessageType);
		}

		/// <summary>
		/// Check message on alert conditions.
		/// </summary>
		/// <param name="message">Message.</param>
		public void Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var schema = _schemas.TryGetValue(message.GetType());

			var type = schema?.AlertType;

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
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_alerts.Close();
			base.DisposeManaged();
		}
	}
}