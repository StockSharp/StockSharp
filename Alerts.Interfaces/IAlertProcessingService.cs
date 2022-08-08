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
	using System.Collections.Generic;

	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Logging;

	/// <summary>
	/// Defines an alert processing service.
	/// </summary>
	public interface IAlertProcessingService : IPersistable, ILogSource
	{
		/// <summary>
		/// All schemas.
		/// </summary>
		IEnumerable<AlertSchema> Schemas { get; }

		/// <summary>
		/// Schema registration event.
		/// </summary>
		event Action<AlertSchema> Registered;

		/// <summary>
		/// Schema unregistering event.
		/// </summary>
		event Action<AlertSchema> UnRegistered;

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

		/// <summary>
		/// Find schema by the specified identifier.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns>Found schema. <see langword="null"/> if schema with the specified identifier doesn't exist.</returns>
		AlertSchema FindSchema(Guid id);
	}
}