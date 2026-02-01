namespace StockSharp.Tests;

using StockSharp.Alerts;

[TestClass]
public class AlertSchemaTemplatesTests
{
	private static readonly SecurityId _testSecId = "AAPL@NASDAQ".ToSecurityId();

	[TestMethod]
	public void PriceAbove_CreatesValidSchema()
	{
		var schema = AlertSchemaTemplates.PriceAbove(_testSecId, 150m);

		schema.AssertNotNull();
		schema.MessageType.AssertEqual(typeof(Level1ChangeMessage));
		schema.AlertType.AssertEqual(AlertNotifications.Sound);
		schema.IsEnabled.AssertTrue();
		schema.Rules.Count.AssertEqual(2);

		// rule 0: security filter
		schema.Rules[0].Operator.AssertEqual(ComparisonOperator.Equal);
		schema.Rules[0].Value.AssertEqual(_testSecId);

		// rule 1: price above 150 → Less operator (Compare(rule.Value, actual) == -1 means rule.Value < actual)
		schema.Rules[1].Operator.AssertEqual(ComparisonOperator.Less);
		schema.Rules[1].Value.AssertEqual(150m);
		schema.Rules[1].Field.ExtraField.AssertEqual(Level1Fields.LastTradePrice);
	}

	[TestMethod]
	public void PriceBelow_CreatesValidSchema()
	{
		var schema = AlertSchemaTemplates.PriceBelow(_testSecId, 100m);

		schema.Rules.Count.AssertEqual(2);
		schema.Rules[1].Operator.AssertEqual(ComparisonOperator.Greater);
		schema.Rules[1].Value.AssertEqual(100m);
		schema.Rules[1].Field.ExtraField.AssertEqual(Level1Fields.LastTradePrice);
	}

	[TestMethod]
	public void BidAbove_UsesCorrectField()
	{
		var schema = AlertSchemaTemplates.BidAbove(_testSecId, 99m);

		schema.Rules[1].Field.ExtraField.AssertEqual(Level1Fields.BestBidPrice);
		schema.Rules[1].Operator.AssertEqual(ComparisonOperator.Less);
		schema.Rules[1].Value.AssertEqual(99m);
	}

	[TestMethod]
	public void AskBelow_UsesCorrectField()
	{
		var schema = AlertSchemaTemplates.AskBelow(_testSecId, 101m);

		schema.Rules[1].Field.ExtraField.AssertEqual(Level1Fields.BestAskPrice);
		schema.Rules[1].Operator.AssertEqual(ComparisonOperator.Greater);
		schema.Rules[1].Value.AssertEqual(101m);
	}

	[TestMethod]
	public void VolumeAbove_UsesCorrectField()
	{
		var schema = AlertSchemaTemplates.VolumeAbove(_testSecId, 1_000_000m);

		schema.Rules[1].Field.ExtraField.AssertEqual(Level1Fields.LastTradeVolume);
		schema.Rules[1].Operator.AssertEqual(ComparisonOperator.Less);
		schema.Rules[1].Value.AssertEqual(1_000_000m);
	}

	[TestMethod]
	public void Level1_CustomField_Works()
	{
		var schema = AlertSchemaTemplates.Level1(_testSecId, Level1Fields.OpenPrice, ComparisonOperator.GreaterOrEqual, 200m);

		schema.Rules[1].Field.ExtraField.AssertEqual(Level1Fields.OpenPrice);
		schema.Rules[1].Operator.AssertEqual(ComparisonOperator.GreaterOrEqual);
		schema.Rules[1].Value.AssertEqual(200m);
	}

	[TestMethod]
	public void PriceAbove_CustomAlertType()
	{
		var schema = AlertSchemaTemplates.PriceAbove(_testSecId, 150m, AlertNotifications.Telegram);

		schema.AlertType.AssertEqual(AlertNotifications.Telegram);
	}

	[TestMethod]
	public void PriceAbove_HasCaption()
	{
		var schema = AlertSchemaTemplates.PriceAbove(_testSecId, 150m);

		schema.Caption.AssertNotNull();
		schema.Caption.Length.AssertGreater(0);
	}

	[TestMethod]
	public void PriceAbove_HasMessage()
	{
		var schema = AlertSchemaTemplates.PriceAbove(_testSecId, 150m);

		schema.Message.AssertNotNull();
		schema.Message.Contains("AAPL").AssertTrue("Message should contain security code");
	}

	[TestMethod]
	public void AllTemplates_HaveUniqueIds()
	{
		var schemas = new[]
		{
			AlertSchemaTemplates.PriceAbove(_testSecId, 100m),
			AlertSchemaTemplates.PriceBelow(_testSecId, 100m),
			AlertSchemaTemplates.BidAbove(_testSecId, 100m),
			AlertSchemaTemplates.AskBelow(_testSecId, 100m),
			AlertSchemaTemplates.VolumeAbove(_testSecId, 100m),
		};

		var ids = schemas.Select(s => s.Id).ToHashSet();
		ids.Count.AssertEqual(schemas.Length, "All schemas must have unique IDs");
	}
}
