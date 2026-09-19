namespace StockSharp.Tests;

/// <summary>
/// Market data storage tests. Split by data type into StorageTests.*.cs; this part holds what they share.
/// </summary>
[TestClass]
public partial class StorageTests : BaseTestClass
{
	private const int _tickCount = 5000;
	private const int _maxRenkoSteps = 100;
	private const int _depthCount1 = 10;
	private const int _depthCount2 = 1000;
	// Depth round-trip fidelity (all serializer branches: prices, volumes, orders count,
	// conditions, nanosec times) is fully exercised at this size.
	private const int _depthCount3 = 2000;
	// Level1 has its own size: every message carries ~115 fields, so what the serializer
	// actually chews through is the field count, not the message count. 1000 messages still
	// give each of the ~115 fields ~500 samples.
	private const int _level1Count = 1000;
	// Order log has its own size as well, so that tuning the depth count cannot silently
	// change the order log scenarios along with it.
	private const int _orderLogCount = 2000;

	private static IStorageRegistry GetStorageRegistry()
	{
		var fs = Helper.MemorySystem;
		return fs.GetStorage(fs.GetSubTemp());
	}
}
