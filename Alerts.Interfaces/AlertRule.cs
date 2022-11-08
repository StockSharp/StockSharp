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
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Rule.
	/// </summary>
	public class AlertRule : IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AlertRule"/>.
		/// </summary>
		public AlertRule()
		{
		}

		/// <summary>
		/// Message property, which will be made a comparison with the value of <see cref="Value"/> based on the criterion <see cref="Operator"/>.
		/// </summary>
		public AlertRuleField Field { get; set; }

		/// <summary>
		/// The criterion of comparison values <see cref="Value"/>.
		/// </summary>
		public ComparisonOperator Operator { get; set; }

		/// <summary>
		/// Comparison value.
		/// </summary>
		public object Value { get; set; }

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Field = storage.GetValue<SettingsStorage>(nameof(Field)).Load<AlertRuleField>();
			Operator = storage.GetValue<ComparisonOperator>(nameof(Operator));

			var value = storage.GetValue<string>(nameof(Value));

			if (storage.GetValue<bool>(nameof(Portfolio)))
				Value = ConfigManager.GetService<IPortfolioProvider>().LookupByPortfolioName(value);
			else
			{
				var valueType = Field.ValueType;

				Value = (valueType == typeof(SecurityId) || valueType == typeof(SecurityId?))
					? ConfigManager.GetService<ISecurityProvider>().LookupByStringId(value)
					: value.To(valueType);
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Field), Field.Save());
			storage.SetValue(nameof(Operator), Operator);

			if (Value is Security security)
				storage.SetValue(nameof(Value), security.Id);
			else if (Value is Portfolio portfolio)
			{
				storage.SetValue(nameof(Portfolio), true);
				storage.SetValue(nameof(Value), portfolio.GetUniqueId());
			}
			else
				storage.SetValue(nameof(Value), Value.ToString());
		}
	}
}