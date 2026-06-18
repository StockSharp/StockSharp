namespace StockSharp.Tests;

using StockSharp.Alerts;

using Ecng.Configuration;

[TestClass]
[DoNotParallelize]
public class AlertProcessingServiceTests
{
	private static readonly MockAlertNotificationService _notificationService = new();

	private AlertProcessingService _service;

	[ClassInitialize]
	public static void ClassInit(TestContext _)
	{
		ConfigManager.RegisterService<IAlertNotificationService>(_notificationService);
	}

	[TestInitialize]
	public void Setup()
	{
		_notificationService.Reset();
		// re-register in case another test class overwrote it
		ConfigManager.RegisterService<IAlertNotificationService>(_notificationService);
		_service = new AlertProcessingService(100);
	}

	[TestCleanup]
	public void Cleanup()
	{
		_service.Dispose();
	}

	[TestMethod]
	public void Register_AddsSchemaToCollection()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 100m);

		_service.Register(schema);

		_service.Schemas.Count().AssertEqual(1);
	}

	[TestMethod]
	public void Register_FiresRegisteredEvent()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 100m);
		AlertSchema fired = null;
		_service.Registered += s => fired = s;

		_service.Register(schema);

		fired.AssertNotNull();
		fired.AssertEqual(schema);
	}

	[TestMethod]
	public void UnRegister_RemovesSchemaFromCollection()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 100m);
		_service.Register(schema);

		((IAlertProcessingService)_service).UnRegister(schema);

		_service.Schemas.Count().AssertEqual(0);
	}

	[TestMethod]
	public void UnRegister_FiresUnRegisteredEvent()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 100m);
		_service.Register(schema);
		AlertSchema fired = null;
		_service.UnRegistered += s => fired = s;

		((IAlertProcessingService)_service).UnRegister(schema);

		fired.AssertNotNull();
	}

	[TestMethod]
	public void FindSchema_ReturnsCorrectSchema()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 100m);
		_service.Register(schema);

		var found = ((IAlertProcessingService)_service).FindSchema(schema.Id);

		found.AssertNotNull();
		found.Id.AssertEqual(schema.Id);
	}

	[TestMethod]
	public void FindSchema_ReturnsNull_WhenNotFound()
	{
		var found = ((IAlertProcessingService)_service).FindSchema(Guid.NewGuid());

		((object)found).AssertNull();
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_TriggersAlert_WhenConditionMet()
	{
		// Domain semantics: the field (actual value) is the left operand, so
		// "Greater 100" must trigger when actual > 100.
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 100m);
		schema.AlertType = AlertNotifications.Log;
		_service.Register(schema);

		// actual=150 > rule.Value=100 → "price Greater 100" is met → must trigger
		var msg = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 150m);
		((IAlertProcessingService)_service).Process(msg);

		await WaitForNotification();

		_notificationService.NotifyCount.AssertEqual(1, "Should have triggered notification");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_DoesNotTrigger_WhenConditionNotMet()
	{
		// Domain semantics: "Greater 200" must trigger only when actual > 200.
		// Here actual=150 is not above 200, so the condition is not met.
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 200m);
		schema.AlertType = AlertNotifications.Log;
		_service.Register(schema);

		var msg = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 150m);
		((IAlertProcessingService)_service).Process(msg);

		await Task.Delay(500);

		_notificationService.NotifyCount.AssertEqual(0, "Should not have triggered notification");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_DoesNotTriggerTwice_SameSchema()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 100m);
		schema.AlertType = AlertNotifications.Log;
		_service.Register(schema);

		var msg1 = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 150m);
		((IAlertProcessingService)_service).Process(msg1);

		await WaitForNotification();

		var msg2 = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 200m);
		((IAlertProcessingService)_service).Process(msg2);

		await Task.Delay(500);

		_notificationService.NotifyCount.AssertEqual(1, "Should trigger only once (activated set)");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_DisabledSchema_DoesNotTrigger()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 200m);
		schema.AlertType = AlertNotifications.Log;
		schema.IsEnabled = false;
		_service.Register(schema);

		var msg = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 250m);
		((IAlertProcessingService)_service).Process(msg);

		await Task.Delay(500);

		_notificationService.NotifyCount.AssertEqual(0, "Disabled schema should not trigger");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_LessOperator_Works()
	{
		// Domain semantics: the field (actual value) is the left operand, so
		// "Less 200" must trigger when actual < 200.
		var schema = CreatePriceSchema(ComparisonOperator.Less, 200m);
		schema.AlertType = AlertNotifications.Log;
		_service.Register(schema);

		// actual=150 < rule.Value=200 → "price Less 200" is met → must trigger
		var msg = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 150m);
		((IAlertProcessingService)_service).Process(msg);

		await WaitForNotification();

		_notificationService.NotifyCount.AssertEqual(1);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_GreaterAndGreaterOrEqual_ConsistentAtThreshold()
	{
		// For a value strictly above the threshold both Greater and
		// GreaterOrEqual must trigger: x > t implies x >= t, so their trigger
		// sets cannot be disjoint. With actual=150 and threshold=100 both rules
		// are satisfied, so two notifications are expected.
		var greater = CreatePriceSchema(ComparisonOperator.Greater, 100m);
		greater.AlertType = AlertNotifications.Log;
		_service.Register(greater);

		var greaterOrEqual = CreatePriceSchema(ComparisonOperator.GreaterOrEqual, 100m);
		greaterOrEqual.AlertType = AlertNotifications.Log;
		_service.Register(greaterOrEqual);

		var msg = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 150m);
		((IAlertProcessingService)_service).Process(msg);

		await WaitForNotification(2);

		_notificationService.NotifyCount.AssertEqual(2, "Greater and GreaterOrEqual must both trigger above the threshold");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_EqualOperator_Works()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Equal, 100m);
		schema.AlertType = AlertNotifications.Log;
		_service.Register(schema);

		var msg = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 100m);
		((IAlertProcessingService)_service).Process(msg);

		await WaitForNotification();

		_notificationService.NotifyCount.AssertEqual(1);
	}

	[TestMethod]
	public void SaveLoad_RoundTrip()
	{
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 42m);
		schema.AlertType = AlertNotifications.Telegram;
		schema.Caption = "test caption";
		schema.Message = "test message";
		_service.Register(schema);

		var storage = new SettingsStorage();
		((IPersistable)_service).Save(storage);

		// create new service and load
		using var service2 = new AlertProcessingService(100);
		((IPersistable)service2).Load(storage);

		service2.Schemas.Count().AssertEqual(1);
		var loaded = service2.Schemas.First();
		loaded.Caption.AssertEqual("test caption");
		loaded.Message.AssertEqual("test message");
		loaded.AlertType.AssertEqual(AlertNotifications.Telegram);
	}

	#region Helpers

	private static AlertSchema CreatePriceSchema(ComparisonOperator op, decimal price)
	{
		var changesProperty = typeof(Level1ChangeMessage).GetProperty(nameof(Level1ChangeMessage.Changes));

		var schema = new AlertSchema(typeof(Level1ChangeMessage))
		{
			AlertType = AlertNotifications.Log,
			Caption = "Test",
			Message = "Test alert",
		};

		schema.Rules.Add(new AlertRule
		{
			Field = new AlertRuleField(changesProperty, Level1Fields.LastTradePrice),
			Operator = op,
			Value = price,
		});

		return schema;
	}

	private static Level1ChangeMessage CreateLevel1Message(string secId, Level1Fields field, decimal value)
	{
		var msg = new Level1ChangeMessage
		{
			SecurityId = secId.ToSecurityId(),
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		msg.Changes.Add(field, value);

		return msg;
	}

	private async Task WaitForNotification(int targetCount = 1, int maxWaitMs = 3000)
	{
		var waited = 0;
		while (_notificationService.NotifyCount < targetCount && waited < maxWaitMs)
		{
			await Task.Delay(50);
			waited += 50;
		}
	}

	#endregion

	#region Mock

	private class MockAlertNotificationService : BaseLogReceiver, IAlertNotificationService
	{
		private int _notifyCount;

		public int NotifyCount => _notifyCount;

		public void Reset() => Interlocked.Exchange(ref _notifyCount, 0);

		public ValueTask NotifyAsync(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref _notifyCount);
			return default;
		}
	}

	#endregion
}
