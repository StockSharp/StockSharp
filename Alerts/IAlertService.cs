#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alerts.Alerts
File: IAlertService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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