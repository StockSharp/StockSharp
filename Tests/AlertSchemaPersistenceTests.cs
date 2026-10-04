namespace StockSharp.Tests;

using StockSharp.Alerts;

/// <summary>
/// Alert schemas are what a user sets up by hand; an application that forgets them on exit asks for the
/// same work at every start.
/// </summary>
[TestClass]
public class AlertSchemaPersistenceTests : BaseTestClass
{
	private static void Unexpected(Exception error)
		=> throw new AssertFailedException("Nothing should have failed here.", error);

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ASchemaThatWasSetUpComesBackAtTheNextStart()
	{
		SettingsStorage stored = null;

		using (var first = new AlertProcessingService(10))
		{
			await using (await AlertSchemaPersistence.StartAsync(first, () => stored, settings => stored = settings, Unexpected, CancellationToken))
			{
				stored.AssertNull("Nothing was stored yet, so nothing was written.");

				first.Register(new AlertSchema(typeof(Level1ChangeMessage)));
			}

			stored.AssertNotNull("Adding a schema is what has to be remembered, and disposing waits for it.");
		}

		using var second = new AlertProcessingService(10);
		await using var persistence = await AlertSchemaPersistence.StartAsync(second, () => stored, _ => { }, Unexpected, CancellationToken);

		second.Schemas.Select(schema => schema.MessageType)
			.SequenceEqual([typeof(Level1ChangeMessage)])
			.AssertTrue("The schema set up in the previous run is there again.");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task RemovingASchemaIsRememberedToo()
	{
		SettingsStorage stored = null;

		using (var service = new AlertProcessingService(10))
		{
			await using (await AlertSchemaPersistence.StartAsync(service, () => stored, settings => stored = settings, Unexpected, CancellationToken))
			{
				var schema = new AlertSchema(typeof(Level1ChangeMessage));

				service.Register(schema);
				((IAlertProcessingService)service).UnRegister(schema);
			}
		}

		stored.AssertNotNull("A change was made, so something was written.");

		using var restored = new AlertProcessingService(10);
		await using var persistence = await AlertSchemaPersistence.StartAsync(restored, () => stored, _ => { }, Unexpected, CancellationToken);

		restored.Schemas.Count().AssertEqual(0, "What was removed does not come back.");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SettingsThatCannotBeReadDoNotStopTheApplication()
	{
		var unreadable = new SettingsStorage();

		unreadable.SetValue("Schemas", "not a schema list at all");

		using var service = new AlertProcessingService(10);

		// Stored settings often come from an earlier version, and a host that throws here cannot start.
		Exception reported = null;

		await using var persistence = await AlertSchemaPersistence.StartAsync(
			service, () => unreadable, _ => { }, error => reported = error, CancellationToken);

		service.Schemas.Count().AssertEqual(0);
		reported.AssertNotNull("What could not be read is said, not swallowed.");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task AWriteThatFailedDoesNotStopTheNextOne()
	{
		SettingsStorage stored = null;
		Exception reported = null;
		var refuse = true;

		using var service = new AlertProcessingService(10);

		await using (await AlertSchemaPersistence.StartAsync(
			service,
			() => null,
			settings =>
			{
				if (refuse)
				{
					refuse = false;
					throw new IOException("the disk is full");
				}

				stored = settings;
			},
			error => reported = error,
			CancellationToken))
		{
			service.Register(new AlertSchema(typeof(Level1ChangeMessage)));

			await WaitAsync(() => reported is not null);

			service.Register(new AlertSchema(typeof(ExecutionMessage)));
		}

		(reported as IOException).AssertNotNull("The failed write was not reported.");
		stored.AssertNotNull("A failed write stopped every write after it.");
	}

	private async Task WaitAsync(Func<bool> condition)
	{
		while (!condition())
			await Task.Delay(10, CancellationToken);
	}
}
