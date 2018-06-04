#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: TraceSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.Diagnostics;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The logs source which receives information from <see cref="Trace"/>.
	/// </summary>
	public class TraceSource : BaseLogSource
	{
		private class TraceListenerEx : TraceListener
		{
			private readonly TraceSource _source;

			public TraceListenerEx(TraceSource source)
			{
				_source = source ?? throw new ArgumentNullException(nameof(source));
			}

			public override void Write(string message)
			{
				_source.RaiseDebugLog(message);
			}

			public override void WriteLine(string message)
			{
				_source.RaiseDebugLog(message);
			}

			public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
			{
				var level = ToStockSharp(eventType);

				if (level == null)
					return;

				_source.RaiseLog(new LogMessage(_source, TimeHelper.NowWithOffset, level.Value, message));
			}

			public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
			{
				var level = ToStockSharp(eventType);

				if (level == null)
					return;

				_source.RaiseLog(new LogMessage(_source, TimeHelper.NowWithOffset, level.Value, format, args));
			}

			private static LogLevels? ToStockSharp(TraceEventType eventType)
			{
				switch (eventType)
				{
					case TraceEventType.Critical:
					case TraceEventType.Error:
						return LogLevels.Error;

					case TraceEventType.Warning:
						return LogLevels.Warning;

					case TraceEventType.Information:
						return LogLevels.Info;

					case TraceEventType.Verbose:
						return LogLevels.Debug;

					case TraceEventType.Start:
					case TraceEventType.Stop:
					case TraceEventType.Suspend:
					case TraceEventType.Resume:
					case TraceEventType.Transfer:
						return null;
					default:
						throw new ArgumentOutOfRangeException(nameof(eventType), eventType, LocalizedStrings.Str1219);
				}
			}
		}

		private void RaiseDebugLog(string message)
		{
			RaiseLog(new LogMessage(this, TimeHelper.NowWithOffset, LogLevels.Debug, message));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TraceSource"/>.
		/// </summary>
		public TraceSource()
		{
			Trace.Listeners.Add(new TraceListenerEx(this));
		}

		/// <summary>
		/// Name.
		/// </summary>
		public override string Name => "Trace";

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Trace.Listeners.Remove(new TraceListenerEx(this));
			base.DisposeManaged();
		}
	}
}