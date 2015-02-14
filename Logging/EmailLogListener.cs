namespace StockSharp.Logging
{
	using System;
	using System.Diagnostics;
	using System.Net.Mail;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Логгер, отсылающий данные на email. 
	/// </summary>
	public class EmailLogListener : LogListener
	{
		private readonly BlockingQueue<Tuple<string, string>> _queue = new BlockingQueue<Tuple<string, string>>();

		private bool _isThreadStarted;

		/// <summary>
		/// Создать <see cref="EmailLogListener"/>.
		/// </summary>
		public EmailLogListener()
		{
		}

		/// <summary>
		/// Адрес, от имени которого будет отравлено сообщение.
		/// </summary>
		public string From { get; set; }

		/// <summary>
		/// Адрес, куда будет отравлено сообщение.
		/// </summary>
		public string To { get; set; }

		/// <summary>
		/// Создать email клиента.
		/// </summary>
		/// <returns>Email клиент.</returns>
		protected virtual SmtpClient CreateClient()
		{
			return new SmtpClient();
		}

		/// <summary>
		/// Создать заголовок.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		/// <returns>Заголовок.</returns>
		protected virtual string GetSubject(LogMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return message.Source.Name + " " + message.Level + " " + message.Time.ToString(TimeFormat);
		}

		/// <summary>
		/// Добавить сообщение в очередь на отправку.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		private void EnqueueMessage(LogMessage message)
		{
			if (message.IsDispose)
			{
				_queue.Close();
				return;
			}

			_queue.Enqueue(Tuple.Create(GetSubject(message), message.Message));

			lock (_queue.SyncRoot)
			{
				if (_isThreadStarted)
					return;

				_isThreadStarted = true;

				ThreadingHelper.Thread(() =>
				{
					try
					{
						using (var email = CreateClient())
						{
							while (true)
							{
								Tuple<string, string> m;

								if (!_queue.TryDequeue(out m))
									break;

								email.Send(From, To, m.Item1, m.Item2);
							}
						}

						lock (_queue.SyncRoot)
							_isThreadStarted = false;
					}
					catch (Exception ex)
					{
						Trace.WriteLine(ex);
					}
				}).Name("Email log queue").Launch();
			}
		}

		/// <summary>
		/// Записать сообщение.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		protected override void OnWriteMessage(LogMessage message)
		{
			EnqueueMessage(message);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			From = storage.GetValue<string>("From");
			To = storage.GetValue<string>("To");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("From", From);
			storage.SetValue("To", To);
		}
	}
}