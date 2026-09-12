namespace StockSharp.Tests;

using StockSharp.Alerts;

using Ecng.Configuration;

[TestClass]
[DoNotParallelize]
public class AlertProcessingServiceTests : BaseTestClass
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

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Load_AfterRegistration_LetsASchemaFireAgain()
	{
		// A delivered alert stops nagging, but loading the saved alerts is the user asking for their
		// configuration back the way they saved it: enabled, and still owing its first delivery. A
		// schema that came back from storage already counted as fired would never alert them again,
		// and nothing in the application would say why.
		var schema = CreatePriceSchema(ComparisonOperator.Greater, 100m);
		schema.AlertType = AlertNotifications.Log;
		_service.Register(schema);

		// actual=150 > 100 -> the rule is met and the alert is delivered.
		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 150m));

		await WaitForNotification();

		_notificationService.NotifyCount.AssertEqual(1, "The first matching message must deliver the alert");

		var storage = new SettingsStorage();
		((IPersistable)_service).Save(storage);
		((IPersistable)_service).Load(storage);

		_service.Schemas.Count().AssertEqual(1, "Load must bring the saved schema back");

		// actual=160 > 100 -> still met, and the reloaded alert owes a delivery of its own.
		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 160m));

		await WaitForNotification(2);

		_notificationService.NotifyCount.AssertEqual(2, "A schema restored by Load must be able to fire again");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Process_KeepsProcessing_AfterRuleEvaluationThrows()
	{
		// A single broken schema must not take the service down: alerts registered
		// alongside it stay entitled to fire on later messages.
		_service.Register(CreateThrowingSchema());

		var healthy = CreatePriceSchema(ComparisonOperator.Greater, 200m);
		_service.Register(healthy);

		// actual=100 is below the healthy threshold of 200, so this message matches
		// nothing - it only makes the broken schema raise.
		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 100m));

		await Task.Delay(500);

		// actual=250 > 200 -> "price Greater 200" is met, so the healthy schema must fire.
		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 250m));

		await WaitForNotification();

		_notificationService.NotifyCount.AssertEqual(1, "A schema whose rule throws must not stop later alerts");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Process_KeepsProcessing_AfterDeliveryFails()
	{
		// A notification transport that refuses one delivery is a transport failure,
		// not a reason to stop: schemas matched later must still be delivered.
		_notificationService.FailNext(1);

		_service.Register(CreatePriceSchema(ComparisonOperator.Equal, 100m));
		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 100m));

		await WaitForAttempts(1);

		_service.Register(CreatePriceSchema(ComparisonOperator.Equal, 200m));
		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 200m));

		await WaitForNotification();

		// two deliveries attempted (one refused, one accepted), one of them delivered.
		_notificationService.AttemptCount.AssertEqual(2, "A refused delivery must not stop later deliveries");
		_notificationService.NotifyCount.AssertEqual(1, "The healthy schema must be delivered after the failed one");
	}

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task Process_BoundsPendingMessages_WhenQueueIsFull()
	{
		// Process is a non-blocking feed. Its configured bound must remain a real memory bound while
		// delivery is stalled: one message is in flight and one waits in this queue.
		using var service = new AlertProcessingService(1);

		// Equal thresholds 100, 200 ... make message i match schema i and nothing
		// else, so we can see exactly which messages survived the bounded queue.
		const int count = 5;

		for (var i = 1; i <= count; i++)
			service.Register(CreatePriceSchema(ComparisonOperator.Equal, i * 100m));

		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_notificationService.Gate = gate;

		try
		{
			((IAlertProcessingService)service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 100m));

			// wait until the first message is out of the queue and held inside delivery
			await WaitForAttempts(1);

			for (var i = 2; i <= count; i++)
				((IAlertProcessingService)service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, i * 100m));
		}
		finally
		{
			_notificationService.Gate = null;
			gate.SetResult();
		}

		await WaitForNotification(2, 10_000);

		_notificationService.NotifyCount.AssertEqual(2, "the bounded queue keeps one pending message while one is being delivered");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Process_DeliversTheSchemaAgain_WhenItsFirstDeliveryFailed()
	{
		// An alert is fulfilled only by a delivery that succeeded. A transport that refused
		// the first attempt has told the user nothing, so the next matching message must
		// carry that same schema again: 1 refused + 1 accepted = 2 attempts, 1 delivery.
		// The delivered alert then stops nagging, so a third matching message adds neither.
		_notificationService.FailNext(1);

		_service.Register(CreatePriceSchema(ComparisonOperator.Greater, 100m));

		// actual=150 > 100 -> the rule is met and delivery is attempted, and refused.
		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 150m));

		await WaitForAttempts(1);

		// actual=160 > 100 -> still met, and the alert is still undelivered.
		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 160m));

		await WaitForNotification();

		_notificationService.NotifyCount.AssertEqual(1, "A refused delivery leaves the alert undelivered, so the next matching message must carry it again");
		_notificationService.AttemptCount.AssertEqual(2, "The schema whose delivery failed must be attempted again");

		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 170m));

		await Task.Delay(500, CancellationToken);

		_notificationService.NotifyCount.AssertEqual(1, "A delivered alert must not be delivered twice");
		_notificationService.AttemptCount.AssertEqual(2, "A delivered alert must not be attempted again");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Process_AttemptsEveryMatchingSchema_AfterFiveRefusedDeliveries()
	{
		// Six enabled schemas match one message, so six deliveries are owed. Refusals by one
		// transport say nothing about the next schema, which may be delivered by another one:
		// counting refusals must not make the service abandon the schemas it never tried.
		const int count = 6;

		// every schema is met by actual=100 > 50, so one message owes all six.
		for (var i = 0; i < count; i++)
			_service.Register(CreatePriceSchema(ComparisonOperator.Greater, 50m));

		// the transport refuses all but one delivery, whichever order they are attempted in.
		_notificationService.FailNext(count - 1);

		((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, 100m));

		await WaitForAttempts(count);

		_notificationService.AttemptCount.AssertEqual(count, "Every schema matched by the message must be attempted, however many earlier deliveries were refused");
		_notificationService.NotifyCount.AssertEqual(1, "The one delivery the transport accepts must reach the user");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Process_KeepsDelivering_AfterFiveRefusedDeliveriesInARow()
	{
		// A transport that was down for five messages in a row is not a reason to stop alerting:
		// once it accepts again, the next matching message must be delivered as usual.
		const int failures = 5;

		// Equal thresholds 100, 200 ... make message i match schema i and nothing else.
		for (var i = 1; i <= failures + 1; i++)
			_service.Register(CreatePriceSchema(ComparisonOperator.Equal, i * 100m));

		_notificationService.FailNext(failures);

		for (var i = 1; i <= failures + 1; i++)
			((IAlertProcessingService)_service).Process(CreateLevel1Message("AAPL@NASDAQ", Level1Fields.LastTradePrice, i * 100m));

		await WaitForNotification();

		_notificationService.AttemptCount.AssertEqual(failures + 1, "Every matching message must be attempted, including the one after five refusals");
		_notificationService.NotifyCount.AssertEqual(1, "The service must still deliver once the transport accepts again");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_DoesNotTrigger_WhenTheMessageDoesNotCarryTheField()
	{
		// A message that says nothing about the last trade price cannot satisfy a rule
		// about it: an absent change is not a value of zero, whatever the threshold.
		var schema = CreatePriceSchema(ComparisonOperator.Less, 100m);
		_service.Register(schema);

		var msg = CreateLevel1Message("AAPL@NASDAQ", Level1Fields.BestBidPrice, 10m);
		((IAlertProcessingService)_service).Process(msg);

		await Task.Delay(500, CancellationToken);

		_notificationService.NotifyCount.AssertEqual(0, "A rule about a change the message does not carry must not be satisfied");
		schema.Rules[0].Field.Invoke(msg).AssertNull();
	}

	[TestMethod]
	public void RuleField_TypesAChangeByWhatTheMessageActuallyCarries()
	{
		// A rule is compared through ValueType, so it has to name the type the change
		// really holds. A position carries Currency as CurrencyTypes, State as
		// PortfolioStates and the order counts as int; only the money-like changes
		// are decimal. Typing them all decimal makes the comparison a cast that fails.
		var changes = typeof(PositionChangeMessage).GetProperty(nameof(PositionChangeMessage.Changes));

		new AlertRuleField(changes, PositionChangeTypes.Currency).ValueType.AssertEqual(typeof(CurrencyTypes));
		new AlertRuleField(changes, PositionChangeTypes.State).ValueType.AssertEqual(typeof(PortfolioStates));
		new AlertRuleField(changes, PositionChangeTypes.OrdersCount).ValueType.AssertEqual(typeof(int));
		new AlertRuleField(changes, PositionChangeTypes.CurrentValue).ValueType.AssertEqual(typeof(decimal));
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_ComparesAnIntegerChange_AsAnInteger()
	{
		// "orders count greater than 5" must fire on a position holding 10 orders: 10 > 5.
		// The change is stored as an int, so a rule typed decimal cannot read it at all.
		_service.Register(CreatePositionSchema(PositionChangeTypes.OrdersCount, ComparisonOperator.Greater, 5));

		((IAlertProcessingService)_service).Process(CreatePositionMessage(PositionChangeTypes.OrdersCount, 10));

		await WaitForNotification();

		_notificationService.NotifyCount.AssertEqual(1, "An integer change must be compared as an integer");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Process_ReadsANonNumericChange_AsItIsStored()
	{
		// A position whose currency is USD must satisfy "currency equals USD"; the change
		// holds a CurrencyTypes value, not a number, and the rule must see it unchanged.
		var schema = CreatePositionSchema(PositionChangeTypes.Currency, ComparisonOperator.Equal, CurrencyTypes.USD);
		_service.Register(schema);

		var msg = CreatePositionMessage(PositionChangeTypes.Currency, CurrencyTypes.USD);
		schema.Rules[0].Field.Invoke(msg).AssertEqual(CurrencyTypes.USD);

		((IAlertProcessingService)_service).Process(msg);

		await WaitForNotification();

		_notificationService.NotifyCount.AssertEqual(1, "A non-numeric change must be readable by a rule about it");
	}

	[TestMethod]
	public void RuleField_SurvivesSaveAndLoad()
	{
		// A saved alert must come back as the same rule - same property, same change, same
		// type to compare through - or the restored rule silently tracks something else.
		AssertRoundTrip(typeof(PositionChangeMessage).GetProperty(nameof(PositionChangeMessage.Changes)), PositionChangeTypes.CurrentValue);
		AssertRoundTrip(typeof(Level1ChangeMessage).GetProperty(nameof(Level1ChangeMessage.Changes)), Level1Fields.LastTradeTime);
		AssertRoundTrip(typeof(Level1ChangeMessage).GetProperty(nameof(Level1ChangeMessage.SecurityId)), null);
	}

	private static void AssertRoundTrip(PropertyInfo property, object extraField)
	{
		var field = new AlertRuleField(property, extraField);

		var restored = field.Clone();

		restored.AssertEqual(field);
		restored.Property.AssertEqual(field.Property);
		restored.ExtraField.AssertEqual(field.ExtraField);
		restored.ValueType.AssertEqual(field.ValueType);
		restored.DisplayName.AssertEqual(field.DisplayName);
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

	private static AlertSchema CreateThrowingSchema()
	{
		var secIdProperty = typeof(Level1ChangeMessage).GetProperty(nameof(Level1ChangeMessage.SecurityId));

		var schema = new AlertSchema(typeof(Level1ChangeMessage))
		{
			AlertType = AlertNotifications.Log,
			Caption = "Broken",
			Message = "Broken alert",
		};

		// the extra field is looked up inside the property value as a dictionary,
		// and SecurityId is not one, so evaluating this rule raises.
		schema.Rules.Add(new AlertRule
		{
			Field = new AlertRuleField(secIdProperty, Level1Fields.LastTradePrice),
			Operator = ComparisonOperator.Greater,
			Value = 100m,
		});

		return schema;
	}

	private static AlertSchema CreatePositionSchema(PositionChangeTypes type, ComparisonOperator op, object value)
	{
		var changesProperty = typeof(PositionChangeMessage).GetProperty(nameof(PositionChangeMessage.Changes));

		var schema = new AlertSchema(typeof(PositionChangeMessage))
		{
			AlertType = AlertNotifications.Log,
			Caption = "Test",
			Message = "Test alert",
		};

		schema.Rules.Add(new AlertRule
		{
			Field = new AlertRuleField(changesProperty, type),
			Operator = op,
			Value = value,
		});

		return schema;
	}

	private static PositionChangeMessage CreatePositionMessage(PositionChangeTypes type, object value)
	{
		var msg = new PositionChangeMessage
		{
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		msg.Changes.Add(type, value);

		return msg;
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

	private async Task WaitForAttempts(int targetCount, int maxWaitMs = 3000)
	{
		var waited = 0;
		while (_notificationService.AttemptCount < targetCount && waited < maxWaitMs)
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
		private int _attemptCount;
		private int _failuresLeft;

		public int NotifyCount => _notifyCount;

		// deliveries started, including the refused ones
		public int AttemptCount => _attemptCount;

		// holds every started delivery until this source is completed
		public TaskCompletionSource Gate { get; set; }

		// makes the next deliveries throw, modelling a transport that is down
		public void FailNext(int count) => Interlocked.Exchange(ref _failuresLeft, count);

		public void Reset()
		{
			Interlocked.Exchange(ref _notifyCount, 0);
			Interlocked.Exchange(ref _attemptCount, 0);
			Interlocked.Exchange(ref _failuresLeft, 0);
			Gate = null;
		}

		public async ValueTask NotifyAsync(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref _attemptCount);

			var gate = Gate;

			if (gate != null)
				await gate.Task.WaitAsync(cancellationToken);

			if (Volatile.Read(ref _failuresLeft) > 0)
			{
				Interlocked.Decrement(ref _failuresLeft);
				throw new InvalidOperationException("Notification transport is down.");
			}

			Interlocked.Increment(ref _notifyCount);
		}
	}

	#endregion
}
