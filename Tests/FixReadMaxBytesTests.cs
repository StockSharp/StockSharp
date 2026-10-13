namespace StockSharp.Tests;

using System.Text;

using StockSharp.Fix.Native;

/// <summary>
/// The incoming FIX read is bounded. The reader accumulates a field's bytes until the SOH
/// terminator, so with MaxBytes at int.MaxValue an unauthenticated connection could stream an
/// unterminated field and grow the buffer to ~2 GB. The byte counter has to run even when the
/// checksum is disabled, or the cap never fires on a checksum-off session.
/// </summary>
[TestClass]
public class FixReadMaxBytesTests : BaseTestClass
{
	[TestMethod]
	public async Task ReadStringAsync_UnterminatedFieldBeyondMaxBytes_Throws_EvenWithChecksumDisabled()
	{
		// 100 bytes, none of them SOH (0x01) — an unterminated field.
		var data = new byte[100];
		for (var i = 0; i < data.Length; i++)
			data[i] = (byte)'A';

		var reader = new TextFixReader(new MemoryStream(data), Encoding.UTF8)
		{
			MaxBytes = 10,
			CheckSumDisabled = true,
		};

		var threw = false;
		try
		{
			await ((IFixReader)reader).ReadStringAsync(CancellationToken);
		}
		catch (InvalidOperationException)
		{
			threw = true;
		}

		threw.AssertTrue("an unterminated field beyond MaxBytes must throw (the cap), not grow unbounded — the counter must run with the checksum off too");
	}
}
