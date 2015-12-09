namespace StockSharp.Alerts
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Alert types.
	/// </summary>
	public enum AlertTypes
	{
		/// <summary>
		/// Sound.
		/// </summary>
		Sound,
		
		/// <summary>
		/// Speech.
		/// </summary>
		Speech,

		/// <summary>
		/// Popup window.
		/// </summary>
		Popup,

		/// <summary>
		/// SMS.
		/// </summary>
		Sms,

		/// <summary>
		/// Email.
		/// </summary>
		Email,

		/// <summary>
		/// Logging.
		/// </summary>
		Log,
	}

	/// <summary>
	/// Defines a alert service.
	/// </summary>
	public interface IAlertService
	{
		/// <summary>
		/// Add alert at the output.
		/// </summary>
		/// <param name="type">Alert type.</param>
		/// <param name="caption">Signal header.</param>
		/// <param name="message">Alert text.</param>
		/// <param name="time">Creation time.</param>
		void PushAlert(AlertTypes type, string caption, string message, DateTimeOffset time);

		/// <summary>
		/// Register schema.
		/// </summary>
		/// <param name="schema">Schema.</param>
		void Register(AlertSchema schema);

		/// <summary>
		/// Remove previously registered by <see cref="Register"/> schema.
		/// </summary>
		/// <param name="schema">Schema.</param>
		void UnRegister(AlertSchema schema);

		/// <summary>
		/// Check message on alert conditions.
		/// </summary>
		/// <param name="message">Message.</param>
		void Process(Message message);
	}
}