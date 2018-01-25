#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: Log4NetLogListener.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Collections;
	using Ecng.Common;

	using log4net;
	using log4net.Config;

	using StockSharp.Localization;

	/// <summary>
	/// Helper class for messages logging based on log4net.
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

			public override string Name => _name;
		}

		private readonly Dictionary<string, Source> _sources = new Dictionary<string,Source>();
		private readonly ILog _log;

		/// <summary>
		/// Initializes a new instance of the <see cref="Log4NetLogger"/>.
		/// </summary>
		/// <param name="configFile">The path to the configuration file log4net.</param>
		public Log4NetLogger(string configFile)
		{
			XmlConfigurator.Configure(new FileInfo(configFile));

			_log = log4net.LogManager.GetLogger("FileAppender");
		}

		internal Log4NetLogListener LogListener { get; set; }

		/// <summary>
		/// To send an information message.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="source">The message source.</param>
		public void Info(string message, string source = "")
		{
			Log(LogLevels.Info, message, source);
		}

		/// <summary>
		/// To send a warning message.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="source">The message source.</param>
		public void Warning(string message, string source = "")
		{
			Log(LogLevels.Warning, message, source);
		}

		/// <summary>
		/// To send an error message.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="source">The message source.</param>
		public void Error(string message, string source = "")
		{
			Log(LogLevels.Error, message, source);
		}

		/// <summary>
		/// To send a debug message.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="source">The message source.</param>
		public void Debug(string message, string source = "")
		{
			Log(LogLevels.Debug, message, source);
		}

		/// <summary>
		/// To send a verbose message.
		/// </summary>
		/// <param name="message">Message text.</param>
		/// <param name="source">The message source.</param>
		public void Verbose(string message, string source = "")
		{
			Log(LogLevels.Verbose, message, source);
		}

		private void Log(LogLevels level, string message, string source)
		{
			WriteMessages(new[] { new LogMessage(_sources.SafeAdd(source, key => new Source(key)), TimeHelper.NowWithOffset, level, message) });
		}

		/// <summary>
		/// To record a message.
		/// </summary>
		/// <param name="message">A debug message.</param>
		protected override void OnWriteMessage(LogMessage message)
		{
			if (message.IsDispose)
			{
				Dispose();
				return;
			}

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
				case LogLevels.Verbose:
				case LogLevels.Debug:
					_log.Debug(str);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.Level, LocalizedStrings.Str1219);
			}
		}
	}

	/// <summary>
	/// Logger sending out messages to <see cref="Log4NetLogger"/>.
	/// </summary>
	public class Log4NetLogListener : ExternalLogListener
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Log4NetLogListener"/>.
		/// </summary>
		public Log4NetLogListener()
			: this("logger.xml")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Log4NetLogListener"/>.
		/// </summary>
		/// <param name="configFile">The path to the configuration file log4net.</param>
		public Log4NetLogListener(string configFile)
			: this(new Log4NetLogger(configFile))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Log4NetLogListener"/>.
		/// </summary>
		/// <param name="logger">Helper class for messages logging based on log4net.</param>
		public Log4NetLogListener(Log4NetLogger logger)
			: base(logger)
		{
			logger.LogListener = this;
		}
	}
}