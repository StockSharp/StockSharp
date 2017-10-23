#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: EmailLogListener.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.Diagnostics;
	using System.Net.Mail;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// The logger sending data to the email.
	/// </summary>
	public class EmailLogListener : LogListener
	{
		private readonly BlockingQueue<Tuple<string, string>> _queue = new BlockingQueue<Tuple<string, string>>();

		private bool _isThreadStarted;

		/// <summary>
		/// Initializes a new instance of the <see cref="EmailLogListener"/>.
		/// </summary>
		public EmailLogListener()
		{
		}

		/// <summary>
		/// The address, on whose behalf the message will be sent.
		/// </summary>
		public string From { get; set; }

		/// <summary>
		/// The address to which the message will be sent to.
		/// </summary>
		public string To { get; set; }

		/// <summary>
		/// To create the email client.
		/// </summary>
		/// <returns>The email client.</returns>
		protected virtual SmtpClient CreateClient()
		{
			return new SmtpClient();
		}

		/// <summary>
		/// To create a header.
		/// </summary>
		/// <param name="message">A debug message.</param>
		/// <returns>Header.</returns>
		protected virtual string GetSubject(LogMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.Source.Name + " " + message.Level + " " + message.Time.ToString(TimeFormat);
		}

		/// <summary>
		/// To add a message in a queue for sending.
		/// </summary>
		/// <param name="message">Message.</param>
		private void EnqueueMessage(LogMessage message)
		{
			if (message.IsDispose)
			{
				Dispose();
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
								if (!_queue.TryDequeue(out var m))
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
		/// To record a message.
		/// </summary>
		/// <param name="message">A debug message.</param>
		protected override void OnWriteMessage(LogMessage message)
		{
			EnqueueMessage(message);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			From = storage.GetValue<string>(nameof(From));
			To = storage.GetValue<string>(nameof(To));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(From), From);
			storage.SetValue(nameof(To), To);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_queue.Close();
			base.DisposeManaged();
		}
	}
}