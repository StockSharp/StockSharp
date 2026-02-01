namespace StockSharp.Alerts;

/// <summary>
/// Factory for creating pre-configured <see cref="AlertSchema"/> from common templates.
/// </summary>
public static class AlertSchemaTemplates
{
	private static AlertRuleField CreateLevel1Field(Level1Fields level1Field)
	{
		var changesProperty = typeof(Level1ChangeMessage).GetProperty(nameof(Level1ChangeMessage.Changes))
			?? throw new InvalidOperationException($"{nameof(Level1ChangeMessage.Changes)} property not found.");

		return new(changesProperty, level1Field);
	}

	private static AlertRuleField CreateSecurityIdField<TMessage>()
		where TMessage : Message
	{
		var property = typeof(TMessage).GetProperty(nameof(ISecurityIdMessage.SecurityId))
			?? throw new InvalidOperationException($"{nameof(ISecurityIdMessage.SecurityId)} property not found on {typeof(TMessage).Name}.");

		return new(property);
	}

	private static AlertSchema CreateLevel1Schema(
		SecurityId securityId,
		Level1Fields level1Field,
		ComparisonOperator op,
		decimal value,
		AlertNotifications alertType,
		string caption,
		string message)
	{
		var schema = new AlertSchema(typeof(Level1ChangeMessage))
		{
			AlertType = alertType,
			Caption = caption,
			Message = message,
		};

		// filter by security
		schema.Rules.Add(new AlertRule
		{
			Field = CreateSecurityIdField<Level1ChangeMessage>(),
			Operator = ComparisonOperator.Equal,
			Value = securityId,
		});

		// filter by value
		schema.Rules.Add(new AlertRule
		{
			Field = CreateLevel1Field(level1Field),
			Operator = op,
			Value = value,
		});

		return schema;
	}

	/// <summary>
	/// Create alert: price above threshold.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="price">Price threshold.</param>
	/// <param name="alertType">Notification type.</param>
	/// <returns>Configured <see cref="AlertSchema"/>.</returns>
	public static AlertSchema PriceAbove(SecurityId securityId, decimal price, AlertNotifications alertType = AlertNotifications.Sound)
		=> CreateLevel1Schema(
			securityId, Level1Fields.LastTradePrice, ComparisonOperator.Less, price, alertType,
			LocalizedStrings.Price + " > " + price,
			$"{securityId.SecurityCode}: {LocalizedStrings.Price} > {price}");

	/// <summary>
	/// Create alert: price below threshold.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="price">Price threshold.</param>
	/// <param name="alertType">Notification type.</param>
	/// <returns>Configured <see cref="AlertSchema"/>.</returns>
	public static AlertSchema PriceBelow(SecurityId securityId, decimal price, AlertNotifications alertType = AlertNotifications.Sound)
		=> CreateLevel1Schema(
			securityId, Level1Fields.LastTradePrice, ComparisonOperator.Greater, price, alertType,
			LocalizedStrings.Price + " < " + price,
			$"{securityId.SecurityCode}: {LocalizedStrings.Price} < {price}");

	/// <summary>
	/// Create alert: best bid price above threshold.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="price">Price threshold.</param>
	/// <param name="alertType">Notification type.</param>
	/// <returns>Configured <see cref="AlertSchema"/>.</returns>
	public static AlertSchema BidAbove(SecurityId securityId, decimal price, AlertNotifications alertType = AlertNotifications.Sound)
		=> CreateLevel1Schema(
			securityId, Level1Fields.BestBidPrice, ComparisonOperator.Less, price, alertType,
			"Bid > " + price,
			$"{securityId.SecurityCode}: Bid > {price}");

	/// <summary>
	/// Create alert: best ask price below threshold.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="price">Price threshold.</param>
	/// <param name="alertType">Notification type.</param>
	/// <returns>Configured <see cref="AlertSchema"/>.</returns>
	public static AlertSchema AskBelow(SecurityId securityId, decimal price, AlertNotifications alertType = AlertNotifications.Sound)
		=> CreateLevel1Schema(
			securityId, Level1Fields.BestAskPrice, ComparisonOperator.Greater, price, alertType,
			"Ask < " + price,
			$"{securityId.SecurityCode}: Ask < {price}");

	/// <summary>
	/// Create alert: volume above threshold.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="volume">Volume threshold.</param>
	/// <param name="alertType">Notification type.</param>
	/// <returns>Configured <see cref="AlertSchema"/>.</returns>
	public static AlertSchema VolumeAbove(SecurityId securityId, decimal volume, AlertNotifications alertType = AlertNotifications.Sound)
		=> CreateLevel1Schema(
			securityId, Level1Fields.LastTradeVolume, ComparisonOperator.Less, volume, alertType,
			LocalizedStrings.Volume + " > " + volume,
			$"{securityId.SecurityCode}: {LocalizedStrings.Volume} > {volume}");

	/// <summary>
	/// Create alert for any <see cref="Level1Fields"/> field.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <param name="field">Level1 field to monitor.</param>
	/// <param name="op">Comparison operator.</param>
	/// <param name="value">Threshold value.</param>
	/// <param name="alertType">Notification type.</param>
	/// <returns>Configured <see cref="AlertSchema"/>.</returns>
	public static AlertSchema Level1(SecurityId securityId, Level1Fields field, ComparisonOperator op, decimal value, AlertNotifications alertType = AlertNotifications.Sound)
		=> CreateLevel1Schema(
			securityId, field, op, value, alertType,
			$"{field.GetDisplayName()} {op.GetDisplayName()} {value}",
			$"{securityId.SecurityCode}: {field.GetDisplayName()} {op.GetDisplayName()} {value}");
}
