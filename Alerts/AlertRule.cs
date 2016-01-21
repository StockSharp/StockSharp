#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alerts.Alerts
File: AlertRule.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Alerts
{
	using System.Reflection;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	/// <summary>
	/// Rule.
	/// </summary>
	public class AlertRule
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AlertRule"/>.
		/// </summary>
		public AlertRule()
		{
		}

		/// <summary>
		/// Message property, which will be made a comparison with the value of <see cref="AlertRule.Value"/> based on the criterion <see cref="AlertRule.Operator"/>.
		/// </summary>
		[Member]
		public PropertyInfo	Property { get; set; }

		/// <summary>
		/// The criterion of comparison values <see cref="AlertRule.Value"/>.
		/// </summary>
		public ComparisonOperator Operator { get; set; }

		/// <summary>
		/// Comparison value.
		/// </summary>
		public object Value { get; set; }
	}
}