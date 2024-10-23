namespace StockSharp.Logging;

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
			return eventType switch
			{
				TraceEventType.Critical or TraceEventType.Error => LogLevels.Error,
				TraceEventType.Warning => LogLevels.Warning,
				TraceEventType.Information => LogLevels.Info,
				TraceEventType.Verbose => LogLevels.Debug,
				TraceEventType.Start or TraceEventType.Stop or TraceEventType.Suspend or TraceEventType.Resume or TraceEventType.Transfer => null,
				_ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, LocalizedStrings.InvalidValue),
			};
		}
	}

	private void RaiseDebugLog(string message)
	{
		RaiseLog(new LogMessage(this, TimeHelper.NowWithOffset, LogLevels.Debug, message));
	}

	private readonly TraceListenerEx _listenerEx;

	/// <summary>
	/// Initializes a new instance of the <see cref="TraceSource"/>.
	/// </summary>
	public TraceSource()
	{
		_listenerEx = new TraceListenerEx(this);
		Trace.Listeners.Add(_listenerEx);
	}

	/// <inheritdoc />
	public override string Name => "Trace";

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		Trace.Listeners.Remove(_listenerEx);
		base.DisposeManaged();
	}
}