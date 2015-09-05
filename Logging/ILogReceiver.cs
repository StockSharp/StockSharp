namespace StockSharp.Logging
{
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
}