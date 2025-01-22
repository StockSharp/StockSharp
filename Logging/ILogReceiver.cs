namespace StockSharp.Logging;

using System;

using Ecng.Common;

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

	/// <summary>
	/// To record a verbose message to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	void LogVerbose(string message, params object[] args);

	/// <summary>
	/// To record a debug message to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	void LogDebug(string message, params object[] args);

	/// <summary>
	/// To record a message to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	void LogInfo(string message, params object[] args);

	/// <summary>
	/// To record a warning to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	void LogWarning(string message, params object[] args);

	/// <summary>
	/// To record an error to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	void LogError(string message, params object[] args);
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

	/// <summary>
	/// To record a verbose message to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	public void LogVerbose(string message, params object[] args)
		=> this.AddVerboseLog(message, args);

	/// <summary>
	/// To record a debug message to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	public void LogDebug(string message, params object[] args)
		=> this.AddDebugLog(message, args);

	/// <summary>
	/// To record a message to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	public void LogInfo(string message, params object[] args)
		=> this.AddInfoLog(message, args);

	/// <summary>
	/// To record a warning to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	public void LogWarning(string message, params object[] args)
		=> this.AddWarningLog(message, args);

	/// <summary>
	/// To record an error to the log.
	/// </summary>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	public void LogError(string message, params object[] args)
		=> this.AddErrorLog(message, args);

	/// <summary>
	/// To record an error to the log.
	/// </summary>
	/// <param name="exception">Error details.</param>
	public void LogError(Exception exception)
		=> this.AddErrorLog(exception);
}

/// <summary>
/// <see cref="BaseLogReceiver"/>.
/// </summary>
public class LogReceiver : BaseLogReceiver
{
	/// <summary>
	/// Create instance.
	/// </summary>
	/// <param name="name">Name.</param>
	public LogReceiver(string name = null)
	{
		if (!name.IsEmptyOrWhiteSpace())
			// ReSharper disable once VirtualMemberCallInConstructor
			Name = name;
	}
}