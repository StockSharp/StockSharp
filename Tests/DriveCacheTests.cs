namespace StockSharp.Tests;

[TestClass]
public class DriveCacheTests : BaseTestClass
{
	[TestMethod]
	public void GetDrive_SamePath_ReturnsSameInstance_AndRaisesEventOnce()
	{
		using var cache = new DriveCache(Helper.MemorySystem);

		var created = 0;
		cache.NewDriveCreated += _ => created++;

		var path = Helper.GetSubTemp();

		var drive1 = cache.GetDrive(path);
		var drive2 = cache.GetDrive(path);

		drive1.AssertNotNull();
		drive1.AssertSame(drive2);
		created.AssertEqual(1);
		cache.Drives.Count().AssertEqual(1);
	}

	[TestMethod]
	public void GetDrive_EmptyPath_ReturnsTryDefaultDrive()
	{
		using var cache = new DriveCache(Helper.MemorySystem);

		cache.GetDrive(Helper.GetSubTemp());

		var drive = cache.GetDrive(string.Empty);

		drive.AssertNotNull();
		drive.AssertSame(cache.TryDefaultDrive);
		drive.AssertSame(cache.DefaultDrive);
	}

	[TestMethod]
	public void DeleteDrive_LastLocal_ThrowsInvalidOperationException()
	{
		using var cache = new DriveCache(Helper.MemorySystem);

		var drive = cache.GetDrive(Helper.GetSubTemp());

		ThrowsExactly<InvalidOperationException>(() => cache.DeleteDrive(drive));
	}

	[TestMethod]
	public void DeleteDrive_RemovesAndRaisesEvent()
	{
		using var cache = new DriveCache(Helper.MemorySystem);

		var drive1 = cache.GetDrive(Helper.GetSubTemp());
		cache.GetDrive(Helper.GetSubTemp());

		var deleted = 0;
		cache.DriveDeleted += _ => deleted++;

		cache.DeleteDrive(drive1);

		deleted.AssertEqual(1);
		cache.Drives.OfType<LocalMarketDataDrive>().Count().AssertEqual(1);
	}

	[TestMethod]
	public void Update_RaisesChanged()
	{
		using var cache = new DriveCache(Helper.MemorySystem);

		var changed = 0;
		cache.Changed += () => changed++;

		cache.Update();

		changed.AssertEqual(1);
	}

	[TestMethod]
	public void SaveLoad_Roundtrip_PreservesDrives()
	{
		using var cache = new DriveCache(Helper.MemorySystem);

		var localPath = Helper.GetSubTemp();
		cache.GetDrive(localPath);

		var remotePath = RemoteMarketDataDrive.DefaultAddress.To<string>();
		var remoteDrive = cache.GetDrive(remotePath);
		IsTrue(remoteDrive is RemoteMarketDataDrive, $"Expected RemoteMarketDataDrive for '{remotePath}', got '{remoteDrive?.GetType().FullName}' (Path='{remoteDrive?.Path}').");

		var storage = new SettingsStorage();
		cache.Save(storage);

		using var cache2 = new DriveCache(Helper.MemorySystem);
		cache2.Load(storage);

		var loaded = cache2.Drives.ToArray();
		var localFullPath = localPath.ToFullPath();
		var localOk = loaded.OfType<LocalMarketDataDrive>().Any(d => d.Path.ToFullPath().ComparePaths(localFullPath));
		IsTrue(localOk, $"Local drive '{localFullPath}' not found. Loaded: {loaded.Select(d => $"{d.GetType().Name}:{d.Path}").JoinComma()}");

		var expectedAddress = ((RemoteMarketDataDrive)remoteDrive).Address;
		var remoteOk = loaded.OfType<RemoteMarketDataDrive>().Any(d => Equals(d.Address, expectedAddress));
		IsTrue(remoteOk, $"Remote drive '{expectedAddress}' not found. Loaded: {loaded.Select(d => $"{d.GetType().Name}:{d.Path}").JoinComma()}");
	}
}
