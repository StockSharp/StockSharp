namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Collections;
	using Ecng.Common;

	using log4net;
	using log4net.Config;

	/// <summary>
	/// Вспомогательный класс для логирования сообщений, основанный на log4net.
	/// </summary>
	public class Log4NetLogger : LogListener
	{
		private sealed class Source : BaseLogSource
		{
			public Source(string name)
			{
				_name = name;
			}

			private readonly string _name;

			public override string Name
			{
				get
				{
					return _name;
				}
			}
		}

		private readonly Dictionary<string, Source> _sources = new Dictionary<string,Source>();
		private readonly ILog _log;

		/// <summary>
		/// Создать <see cref="Log4NetLogger"/>.
		/// </summary>
		/// <param name="configFile">Путь к конфигурационному файлу log4net.</param>
		public Log4NetLogger(string configFile)
		{
			XmlConfigurator.Configure(new FileInfo(configFile));

			_log = log4net.LogManager.GetLogger("FileAppender");
		}

		internal Log4NetLogListener LogListener { get; set; }

		///<summary>
		/// Отравить информационное сообщение.
		///</summary>
		///<param name="message">Текст сообщения.</param>
		///<param name="source">Источник сообщения.</param>
		public void Info(string message, string source = "")
		{
			Log(LogLevels.Info, message, source);
		}

		///<summary>
		/// Отправить сообщение-предупреждение.
		///</summary>
		///<param name="message">Текст сообщения.</param>
		///<param name="source">Источник сообщения.</param>
		public void Warning(string message, string source = "")
		{
			Log(LogLevels.Warning, message, source);
		}

		///<summary>
		/// Отправить сообщение об ошибке.
		///</summary>
		///<param name="message">Текст сообщения.</param>
		///<param name="source">Источник сообщения.</param>
		public void Error(string message, string source = "")
		{
			Log(LogLevels.Error, message, source);
		}

		///<summary>
		/// Отправить отладочное сообщение.
		///</summary>
		///<param name="message">Текст сообщения.</param>
		///<param name="source">Источник сообщения.</param>
		public void Debug(string message, string source = "")
		{
			Log(LogLevels.Debug, message, source);
		}

		private void Log(LogLevels level, string message, string source)
		{
			WriteMessages(new[] { new LogMessage(_sources.SafeAdd(source, key => new Source(key)), TimeHelper.Now, level, message) });
		}

		/// <summary>
		/// Записать сообщение.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		protected override void OnWriteMessage(LogMessage message)
		{
			var str = "{0} | {1,-15} | {2}".Put(message.Time.ToString(LogListener.TimeFormat), message.Source, message.Message);

			switch (message.Level)
			{
				case LogLevels.Info:
					_log.Info(str);
					break;
				case LogLevels.Warning:
					_log.Warn(str);
					break;
				case LogLevels.Error:
					_log.Error(str);
					break;
				case LogLevels.Debug:
					_log.Debug(str);
					break;
				default:
					throw new ArgumentOutOfRangeException("message");
			}
		}
	}

	/// <summary>
	/// Логгер, отсылающий сообщения в <see cref="Log4NetLogger"/>.
	/// </summary>
	public class Log4NetLogListener : ExternalLogListener
	{
		/// <summary>
		/// Создать <see cref="Log4NetLogListener"/>.
		/// </summary>
		public Log4NetLogListener()
			: this("logger.xml")
		{
		}

		/// <summary>
		/// Создать <see cref="Log4NetLogListener"/>.
		/// </summary>
		/// <param name="configFile">Путь к конфигурационному файлу log4net.</param>
		public Log4NetLogListener(string configFile)
			: this(new Log4NetLogger(configFile))
		{
		}

		/// <summary>
		/// Создать <see cref="Log4NetLogListener"/>.
		/// </summary>
		/// <param name="logger">Вспомогательный класс для логирования сообщений, основанный на log4net.</param>
		public Log4NetLogListener(Log4NetLogger logger)
			: base(logger)
		{
			logger.LogListener = this;
		}
	}
}