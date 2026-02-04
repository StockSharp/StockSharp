namespace StockSharp.Tests;

using StockSharp.Alerts;
using StockSharp.Algo.Strategies;

[TestClass]
public class StrategyAlertTests : BaseTestClass
{
	private class MockAlertService : BaseLogReceiver, IAlertNotificationService
	{
		public List<(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time)> Notifications { get; } = [];

		public ValueTask NotifyAsync(AlertNotifications type, long? externalId, LogLevels logLevel, string caption, string message, DateTime time, CancellationToken cancellationToken)
		{
			Notifications.Add((type, externalId, logLevel, caption, message, time));
			return ValueTask.CompletedTask;
		}
	}

	private class TestStrategy : Strategy
	{
		public void TestAlert(AlertNotifications type, string caption, string message)
			=> Alert(type, caption, message);

		public void TestAlertPopup(string message)
			=> AlertPopup(message);

		public void TestAlertSound(string message)
			=> AlertSound(message);

		public void TestAlertLog(string message)
			=> AlertLog(message);
	}

	[TestMethod]
	public async Task Alert_WithService_SendsNotification()
	{
		var strategy = new TestStrategy { Name = "TestStrategy" };
		var mockService = new MockAlertService();
		strategy.SetAlertService(mockService);

		strategy.TestAlert(AlertNotifications.Popup, "TestCaption", "TestMessage");

		// Wait for async task to complete
		await Task.Delay(50, CancellationToken);

		AreEqual(1, mockService.Notifications.Count, "Should have one notification");
		var (type, _, _, caption, message, _) = mockService.Notifications[0];
		AreEqual(AlertNotifications.Popup, type);
		AreEqual("TestCaption", caption);
		AreEqual("TestMessage", message);
	}

	[TestMethod]
	public void Alert_WithoutService_DoesNotThrow()
	{
		var strategy = new TestStrategy { Name = "TestStrategy" };
		// No alert service set

		// Should not throw
		strategy.TestAlert(AlertNotifications.Popup, "TestCaption", "TestMessage");
	}

	[TestMethod]
	public async Task AlertPopup_SendsPopupNotification()
	{
		var strategy = new TestStrategy { Name = "TestStrategy" };
		var mockService = new MockAlertService();
		strategy.SetAlertService(mockService);

		strategy.TestAlertPopup("PopupMessage");

		await Task.Delay(50, CancellationToken);

		AreEqual(1, mockService.Notifications.Count);
		AreEqual(AlertNotifications.Popup, mockService.Notifications[0].type);
		AreEqual("PopupMessage", mockService.Notifications[0].message);
		AreEqual("TestStrategy", mockService.Notifications[0].caption);
	}

	[TestMethod]
	public async Task AlertSound_SendsSoundNotification()
	{
		var strategy = new TestStrategy { Name = "TestStrategy" };
		var mockService = new MockAlertService();
		strategy.SetAlertService(mockService);

		strategy.TestAlertSound("SoundMessage");

		await Task.Delay(50, CancellationToken);

		AreEqual(1, mockService.Notifications.Count);
		AreEqual(AlertNotifications.Sound, mockService.Notifications[0].type);
	}

	[TestMethod]
	public async Task AlertLog_SendsLogNotification()
	{
		var strategy = new TestStrategy { Name = "TestStrategy" };
		var mockService = new MockAlertService();
		strategy.SetAlertService(mockService);

		strategy.TestAlertLog("LogMessage");

		await Task.Delay(50, CancellationToken);

		AreEqual(1, mockService.Notifications.Count);
		AreEqual(AlertNotifications.Log, mockService.Notifications[0].type);
	}

	[TestMethod]
	public void GetSetAlertService_WorksCorrectly()
	{
		var strategy = new TestStrategy();
		var mockService = new MockAlertService();

		IsNull(strategy.GetAlertService(), "Should be null initially");

		strategy.SetAlertService(mockService);

		AreEqual(mockService, strategy.GetAlertService(), "Should return the set service");
	}
}
