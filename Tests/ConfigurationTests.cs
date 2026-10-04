namespace StockSharp.Tests;

[TestClass]
public class ConfigurationTests : BaseTestClass
{
	[TestMethod]
	public async Task SubscriptionConfig_RoundTrip_KeepsEverySetting()
	{
		// A tick is 100ns, so the fraction below carries both microseconds and the odd 7 ticks.
		var from = new DateTime(2026, 3, 1, 23, 45, 30, DateTimeKind.Utc).AddTicks(1234567);
		var to = from.AddDays(1).AddTicks(7);

		var config = new SubscriptionConfig
		{
			Security = "SBER@TQBR".ToSecurityId(),
			DataType = DataType.Create(typeof(PnFCandleMessage), new PnFArg { BoxSize = new Unit(2), ReversalAmount = 3 }),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = TimeSpan.FromMinutes(5).TimeFrame(),
			BuildField = Level1Fields.BestBidPrice,
			From = from,
			To = to,
			// Above int.MaxValue: a count that must not come back narrowed.
			Count = 12345678901,
			MaxDepth = 20,
		};

		var storage = await config.SaveAsync(CancellationToken);

		// Times are persisted as ticks, so the stored number is the tick count itself.
		storage.GetValue<long>(nameof(SubscriptionConfig.From)).AssertEqual(from.Ticks);
		storage.GetValue<long>(nameof(SubscriptionConfig.To)).AssertEqual(to.Ticks);

		var loaded = await storage.LoadAsync<SubscriptionConfig>(CancellationToken);

		loaded.Security.AssertEqual(config.Security);
		loaded.DataType.AssertEqual(config.DataType);
		loaded.BuildMode.AssertEqual(config.BuildMode);
		loaded.BuildFrom.AssertEqual(config.BuildFrom);
		loaded.BuildField.AssertEqual(config.BuildField);
		loaded.From.Value.AssertEqual(from);
		loaded.To.Value.AssertEqual(to);
		loaded.Count.Value.AssertEqual(12345678901L);
		loaded.MaxDepth.Value.AssertEqual(20);

		// DateTime equality ignores Kind, so the interval is checked for being UTC separately:
		// a stored interval read back as local time would request a different window.
		loaded.From.Value.Kind.AssertEqual(DateTimeKind.Utc);
		loaded.To.Value.Kind.AssertEqual(DateTimeKind.Utc);

		// The record compares by value, so this also catches a property added later and never saved.
		loaded.AssertEqual(config);
	}

	[TestMethod]
	public async Task SubscriptionConfig_RoundTrip_LeavesUnsetOptionsUnset()
	{
		// Only the data type is chosen; everything else is "no opinion" and must stay that way,
		// because a default that appears out of nowhere silently narrows the subscription.
		var config = new SubscriptionConfig { DataType = DataType.Level1 };

		var loaded = await (await config.SaveAsync(CancellationToken)).LoadAsync<SubscriptionConfig>(CancellationToken);

		loaded.DataType.AssertEqual(DataType.Level1);
		loaded.Security.AssertNull();
		loaded.BuildMode.AssertNull();
		loaded.BuildFrom.AssertNull();
		loaded.BuildField.AssertNull();
		loaded.From.AssertNull();
		loaded.To.AssertNull();
		loaded.Count.AssertNull();
		loaded.MaxDepth.AssertNull();

		loaded.AssertEqual(config);
	}

	[TestMethod]
	public async Task SubscriptionConfig_Load_RefusesADataTypeItCannotRead()
	{
		var storage = new SettingsStorage();
		storage.Set(nameof(SubscriptionConfig.DataType), "NoSuchMessage:0");

		// A config naming a data type that does not exist must not load as a usable subscription.
		await ThrowsAsync<Exception>(async () => await storage.LoadAsync<SubscriptionConfig>(CancellationToken));
	}

	[TestMethod]
	[DoNotParallelize] // AppTime.TimeZone is process-wide state.
	public void AppTime_FromUtc_MovesTheInstantIntoTheConfiguredZone()
	{
		var original = AppTime.TimeZone;

		try
		{
			AppTime.TimeZone = TimeZoneInfo.CreateCustomTimeZone("cfg-test-0530", TimeSpan.FromMinutes(330), "cfg", "cfg");

			// 2026-03-01 23:45:30.1234567 UTC plus 5h30m crosses midnight into 2026-03-02 05:15:30.1234567.
			var utc = new DateTime(2026, 3, 1, 23, 45, 30, DateTimeKind.Utc).AddTicks(1234567);
			var expected = new DateTime(2026, 3, 2, 5, 15, 30).AddTicks(1234567);

			AppTime.FromUtc(utc).AssertEqual(expected);

			// The method is named for what it takes, so a value that merely forgot to say it is UTC
			// must not be re-interpreted as machine-local time.
			AppTime.FromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)).AssertEqual(expected);

			var expectedDto = new DateTimeOffset(expected, TimeSpan.FromMinutes(330));
			var utcDto = new DateTimeOffset(utc.Ticks, TimeSpan.Zero);

			// The offset form keeps the instant and states the application offset.
			AppTime.FromUtc(utcDto).Offset.AssertEqual(TimeSpan.FromMinutes(330));
			AppTime.FromUtc(utcDto).AssertEqual(expectedDto);
			AppTime.ToApp(utcDto).Offset.AssertEqual(TimeSpan.FromMinutes(330));
			AppTime.ToApp(utcDto).AssertEqual(expectedDto);

			// Formatting follows the current culture, so it is fixed here: what is under test is
			// that the text carries the converted time and not the UTC one.
			using (Do.WithCulture(CultureInfo.InvariantCulture))
				AppTime.FormatFromUtc(utc, "yyyy-MM-dd HH:mm:ss").AssertEqual("2026-03-02 05:15:30");
		}
		finally
		{
			AppTime.TimeZone = original;
		}
	}

	[TestMethod]
	[DoNotParallelize] // AppTime.TimeZone is process-wide state.
	public void AppTime_FromUtc_HonoursDaylightSaving()
	{
		var original = AppTime.TimeZone;

		try
		{
			// Base offset +2, plus one hour between March 1st and November 1st.
			var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
				new DateTime(2000, 1, 1),
				new DateTime(2100, 12, 31),
				TimeSpan.FromHours(1),
				TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 1),
				TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 3, 0, 0), 11, 1));

			AppTime.TimeZone = TimeZoneInfo.CreateCustomTimeZone("cfg-test-dst", TimeSpan.FromHours(2), "cfg", "cfg", "cfg-dst", [rule]);

			// January is outside the rule: 12:00 UTC + 2h = 14:00.
			AppTime.FromUtc(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)).AssertEqual(new DateTime(2026, 1, 15, 14, 0, 0));

			// July is inside it: 12:00 UTC + 2h + 1h = 15:00.
			AppTime.FromUtc(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)).AssertEqual(new DateTime(2026, 7, 15, 15, 0, 0));
		}
		finally
		{
			AppTime.TimeZone = original;
		}
	}

	[TestMethod]
	[DoNotParallelize] // AppTime.TimeZone is process-wide state.
	public void AppTime_ToAppTime_KeepsTheInstant()
	{
		var original = AppTime.TimeZone;

		try
		{
			var zone = TimeZoneInfo.CreateCustomTimeZone("cfg-test-0400", TimeSpan.FromHours(4), "cfg", "cfg");
			AppTime.TimeZone = zone;

			// A UTC value moves by the configured offset: 08:00 UTC + 4h = 12:00.
			new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc).ToAppTime().AssertEqual(new DateTime(2026, 6, 10, 12, 0, 0));

			// A value already on the machine clock names the same instant afterwards, whatever
			// zone this machine is in; 08:00 is far from any daylight-saving transition.
			var local = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Local);
			var app = DateTime.SpecifyKind(local.ToAppTime(), DateTimeKind.Unspecified);

			TimeZoneInfo.ConvertTimeToUtc(app, zone).AssertEqual(local.ToUniversalTime());
		}
		finally
		{
			AppTime.TimeZone = original;
		}
	}

	[TestMethod]
	[DoNotParallelize] // AppTime.TimeZone is process-wide state.
	public void AppTime_ToAppTime_ReadsAKindlessValueAsUtc()
	{
		var original = AppTime.TimeZone;

		try
		{
			AppTime.TimeZone = TimeZoneInfo.CreateCustomTimeZone("cfg-test-0700", TimeSpan.FromHours(7), "cfg", "cfg");

			var utc = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);
			var kindless = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);

			// 08:00 UTC + 7h = 15:00 in the configured zone.
			var expected = new DateTime(2026, 6, 10, 15, 0, 0);

			utc.ToAppTime().AssertEqual(expected);

			// Every moment in this codebase is UTC, and a value that lost its Kind on the way
			// through storage or a wire format still names the same instant - AppTime.FromUtc
			// already says exactly that, and the two must not disagree about the same value.
			var note = $"machine local offset is {TimeZoneInfo.Local.GetUtcOffset(utc)}";

			kindless.ToAppTime().AssertEqual(expected, note);
			kindless.ToAppTime().AssertEqual(AppTime.FromUtc(kindless), note);
		}
		finally
		{
			AppTime.TimeZone = original;
		}
	}

	[TestMethod]
	public void AppTime_TimeZone_RefusesNull()
	{
		// Every conversion in the application reads this, so it must never become null.
		ThrowsExactly<ArgumentNullException>(() => AppTime.TimeZone = null);
	}

	[TestMethod]
	[DoNotParallelize] // The working directory is process-wide state.
	public void InMemoryMessageAdapterProvider_PossibleAdapters_DoNotDependOnTheWorkingDirectory()
	{
		// The provider scans a directory with Directory.GetFiles, so a real one is needed here.
		var fs = LocalFileSystem.Instance;

		var withAdapters = fs.CreateTempDir();
		var elsewhere = fs.CreateTempDir();

		// A file name that passes the "StockSharp.*" filter over content that really does carry
		// adapters, so the scan has something to find and the comparison below is not vacuous.
		File.Copy(typeof(FindAdaptersTestValidAdapter).Assembly.Location, Path.Combine(withAdapters, "StockSharp.CwdProbe.dll"));

		var saved = Directory.GetCurrentDirectory();

		try
		{
			Directory.SetCurrentDirectory(withAdapters);
			var found = AdapterNames();

			found.Length.AssertGreater(0, "the probe directory produced no adapters, so the check below proves nothing");

			Directory.SetCurrentDirectory(elsewhere);

			// What a user may choose from is what ships with the application; the same binaries
			// started from another folder - a service, a CLI - must offer the same adapters.
			AdapterNames().AssertEqual(found);
		}
		finally
		{
			Directory.SetCurrentDirectory(saved);
		}

		static string[] AdapterNames()
			=> [.. new InMemoryMessageAdapterProvider([]).PossibleAdapters.Select(a => a.GetType().FullName).OrderBy(n => n)];
	}

	private static SettingsStorage CreateCultureSensitiveSettings()
	{
		var settings = new SettingsStorage();

		settings.Set("price", 1234.5678m);
		settings.Set("ratio", 0.15d);
		settings.Set("stamp", new DateTime(2026, 3, 1, 23, 45, 30, DateTimeKind.Utc).AddTicks(1234567));
		settings.Set("span", TimeSpan.FromMinutes(90));
		settings.Set("count", 12345678901L);

		return settings;
	}

	[TestMethod]
	public async Task InvariantSerializer_WritesTheSameTextInAnyCulture()
	{
		var settings = CreateCultureSensitiveSettings();

		var reference = (await settings.SerializeInvariantAsync(false, CancellationToken)).UTF8();

		// ru-RU writes a decimal comma, de-DE a dotted date, th-TH a Buddhist year (2569, not 2026).
		// A config file must read the same on every machine, so none of that may reach the file.
		foreach (var name in new[] { "ru-RU", "de-DE", "th-TH" })
		{
			string written;

			using (Do.WithCulture(CultureInfo.GetCultureInfo(name)))
				written = (await settings.SerializeInvariantAsync(false, CancellationToken)).UTF8();

			written.AssertEqual(reference, name);
		}
	}

	[TestMethod]
	public async Task InvariantSerializer_ReadsBackWhatItWroteUnderAnotherCulture()
	{
		var fs = new MemoryFileSystem();
		var fileName = "/settings.json";
		var stamp = new DateTime(2026, 3, 1, 23, 45, 30, DateTimeKind.Utc).AddTicks(1234567);

		using (Do.WithCulture(CultureInfo.GetCultureInfo("ru-RU")))
			await CreateCultureSensitiveSettings().SerializeInvariantAsync(fs, fileName, true, CancellationToken);

		SettingsStorage restored;

		// Written on one machine, read on another with a different culture.
		using (Do.WithCulture(CultureInfo.GetCultureInfo("th-TH")))
			restored = await fs.DeserializeInvariantAsync(fileName, CancellationToken);

		restored.GetValue<decimal>("price").AssertEqual(1234.5678m);
		restored.GetValue<double>("ratio").AssertEqual(0.15d);
		restored.GetValue<TimeSpan>("span").AssertEqual(TimeSpan.FromMinutes(90));
		restored.GetValue<long>("count").AssertEqual(12345678901L);

		var readStamp = restored.GetValue<DateTime>("stamp");

		readStamp.AssertEqual(stamp);
		readStamp.Kind.AssertEqual(DateTimeKind.Utc);
	}

	[TestMethod]
	public async Task InvariantSerializer_BomIsTheOnlyDifferenceBetweenTheTwoForms()
	{
		var settings = CreateCultureSensitiveSettings();

		var withBom = (await settings.SerializeInvariantAsync(true, CancellationToken));
		var withoutBom = (await settings.SerializeInvariantAsync(false, CancellationToken));

		// The UTF8 preamble is three bytes and nothing else about the file changes.
		withBom.Length.AssertEqual(withoutBom.Length + 3);

		// Whether the preamble is there or not, the same settings come back.
		(await withBom.DeserializeInvariantAsync(CancellationToken)).GetValue<decimal>("price").AssertEqual(1234.5678m);
		(await withoutBom.DeserializeInvariantAsync(CancellationToken)).GetValue<decimal>("price").AssertEqual(1234.5678m);
	}

	// A language other than the one the process is running in, so a value that never travelled
	// cannot pass the round trip by coinciding with the default.
	private static string AnotherLanguage()
		=> new AppStartSettings().Language.EqualsIgnoreCase("en") ? "ru" : "en";

	// A zone other than this machine's, for the same reason.
	private static TimeZoneInfo AnotherTimeZone()
		=> TimeZoneInfo.Local.Id == TimeZoneInfo.Utc.Id
			? TimeZoneInfo.GetSystemTimeZones().First(z => z.Id != TimeZoneInfo.Local.Id)
			: TimeZoneInfo.Utc;

	/// <summary>
	/// These three are what the user chose before the application even opened: the language it
	/// speaks, whether it goes online, and the zone every time is shown in. A choice that does not
	/// survive being written down is silently taken back on the next start, and the application
	/// comes up speaking another language or stamping times hours away from the ones just seen.
	/// </summary>
	[TestMethod]
	public async Task AppStartSettings_RoundTrip_KeepsTheChoicesTheUserMade()
	{
		var language = AnotherLanguage();
		var zone = AnotherTimeZone();

		var settings = new AppStartSettings
		{
			Language = language,
			Online = false,
			TimeZone = zone,
		};

		var loaded = await (await settings.SaveAsync(CancellationToken)).LoadAsync<AppStartSettings>(CancellationToken);

		loaded.Language.AssertEqual(language);
		loaded.Online.AssertFalse("the user asked to start offline");
		loaded.TimeZone.Id.AssertEqual(zone.Id);
	}

	/// <summary>
	/// Time-zone identifiers are not the same on every platform - a file written on Windows names
	/// "Russian Standard Time", one written on Linux names "Europe/Moscow" - so a configuration
	/// carried between machines can name a zone this one has never heard of. That costs the user
	/// the zone, and nothing else: the application still has to start, in the language and the
	/// connection mode it was left in, with a usable zone rather than none.
	/// </summary>
	[TestMethod]
	public async Task AppStartSettings_Load_KeepsTheOtherSettingsWhenTheTimeZoneIsUnknownHere()
	{
		var language = AnotherLanguage();

		var storage = new SettingsStorage();

		storage.Set(nameof(AppStartSettings.Language), language);
		storage.Set(nameof(AppStartSettings.Online), false);
		storage.Set(nameof(AppStartSettings.TimeZone), "No/Such_Zone_On_This_Machine");

		var loaded = await storage.LoadAsync<AppStartSettings>(CancellationToken);

		loaded.Language.AssertEqual(language);
		loaded.Online.AssertFalse("an unreadable time zone must not drag the connection mode with it");
		loaded.TimeZone.AssertNotNull("every time in the application is shown through this zone, so it must never be left unset");
	}

	/// <summary>
	/// The very first start has no configuration file. Saying so plainly is what lets the caller
	/// fall back to defaults; an exception or a half-built object would turn a fresh installation
	/// into a crash on launch.
	/// </summary>
	[TestMethod]
	public async Task AppStartSettings_TryLoad_SaysThereIsNothingSavedOnAFirstStart()
	{
		(await AppStartSettings.TryLoadAsync(new MemoryFileSystem(), CancellationToken)).AssertNull();
	}

	/// <summary>
	/// The settings are written on exit and read on the next launch. This is the whole point of the
	/// pair: what one wrote, the other has to find and bring back unchanged.
	/// </summary>
	[TestMethod]
	public async Task AppStartSettings_TrySave_IsReadBackByTryLoad()
	{
		var fs = new MemoryFileSystem();

		var language = AnotherLanguage();
		var zone = AnotherTimeZone();

		await new AppStartSettings
		{
			Language = language,
			Online = false,
			TimeZone = zone,
		}.TrySaveAsync(fs, CancellationToken);

		var loaded = await AppStartSettings.TryLoadAsync(fs, CancellationToken);

		loaded.AssertNotNull("the settings were just written, so the next start must find them");
		loaded.Language.AssertEqual(language);
		loaded.Online.AssertFalse();
		loaded.TimeZone.Id.AssertEqual(zone.Id);
	}

	/// <summary>
	/// Every moment the application shows is converted through this zone, so it can never be absent.
	/// Refusing the assignment keeps the failure at the line that caused it, instead of letting a
	/// null reach the first conversion and crash somewhere that says nothing about why.
	/// </summary>
	[TestMethod]
	public void AppStartSettings_TimeZone_RefusesNull()
	{
		ThrowsExactly<ArgumentNullException>(() => new AppStartSettings().TimeZone = null);
	}

	/// <summary>
	/// Both methods exist to read and write a file, and the file system is the only way they can do
	/// it. Naming the missing argument tells the caller what to supply; a NullReferenceException
	/// from somewhere inside does not.
	/// </summary>
	[TestMethod]
	public async Task AppStartSettings_RefusesToWorkWithoutAFileSystem()
	{
		await ThrowsExactlyAsync<ArgumentNullException>(() => AppStartSettings.TryLoadAsync(null, CancellationToken).AsTask());
		await ThrowsExactlyAsync<ArgumentNullException>(() => new AppStartSettings().TrySaveAsync(null, CancellationToken).AsTask());
	}
}
