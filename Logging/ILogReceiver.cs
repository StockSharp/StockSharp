#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: ILogReceiver.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;

	/// <summary>
	/// Logs recipient interface.
	/// </summary>
	public interface ILogReceiver : ILogSource
	{
		/// <summary>
		/// To record a message to the log.
		/// </summary>
		/// <param name="message">A debug message.</param>
		void AddLog(LogMessage message);
	}

	/// <summary>
	/// The base implementation <see cref="ILogReceiver"/>.
	/// </summary>
	public abstract class BaseLogReceiver : BaseLogSource, ILogReceiver
	{
		/// <summary>
		/// Initialize <see cref="BaseLogReceiver"/>.
		/// </summary>
		protected BaseLogReceiver()
		{
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			RaiseLog(message);
		}
	}

	/// <summary>
	/// Global logs receiver.
	/// </summary>
	public class GlobalLogReceiver : ILogReceiver
	{
		private ILogReceiver App => LogManager.Instance?.Application;

		private GlobalLogReceiver()
		{
		}

		/// <summary>
		/// Instance.
		/// </summary>
		public static GlobalLogReceiver Instance { get; } = new GlobalLogReceiver();

		Guid ILogSource.Id => App?.Id ?? default;

		string ILogSource.Name
		{
			get => App?.Name;
			set { }
		}
		
		ILogSource ILogSource.Parent
		{
			get => App?.Parent;
			set => throw new NotSupportedException();
		}

		/// <inheritdoc />
		public event Action<ILogSource> ParentRemoved
		{
			add { }
			remove { }
		}
		
		LogLevels ILogSource.LogLevel
		{
			get => App?.LogLevel ?? default;
			set
			{
				var app = App;

				if (app == null)
					return;

				app.LogLevel = value;
			}
		}

		DateTimeOffset ILogSource.CurrentTime => App?.CurrentTime ?? default;

		bool ILogSource.IsRoot => true;

		event Action<LogMessage> ILogSource.Log
		{
			add
			{
				var app = App;

				if (app == null)
					return;

				app.Log += value;
			}
			remove
			{
				var app = App;

				if (app == null)
					return;

				app.Log -= value;
			}
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			App?.AddLog(message);
		}

		void IDisposable.Dispose()
		{
		}
	}
}