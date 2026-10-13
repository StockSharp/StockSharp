namespace StockSharp.Tests;

using System.Text;

using StockSharp.Fix.Native;

using FixExtensions = StockSharp.Fix.Native.Extensions;

/// <summary>
/// An aborted serialization resets the reused per-session body writer. WriteFixMessageAsync resets
/// it only on success, so a body handler that throws mid-way left partial bytes at a non-zero
/// position: the next message was appended after them and shipped as one BodyLength- and
/// checksum-consistent frame that silently swallowed a real message.
/// </summary>
[TestClass]
public class FixMessageWriteAbortTests : BaseTestClass
{
	[TestMethod]
	public async Task WriteFixMessageAsync_HandlerThrows_ResetsBodyWriter()
	{
		var writer = new TextFixWriter(new MemoryStream(), Encoding.UTF8, ownsStream: true);
		var bodyWriter = new TextFixWriter(new MemoryStream(), Encoding.UTF8, ownsStream: true);
		var parser = new FastDateTimeParser(FixExtensions.TimeStampFormat);

		var threw = false;
		try
		{
			await writer.WriteFixMessageAsync(bodyWriter, "FIX.4.4", "0", "S", "T", parser, 1,
				(bw, ct) => throw new InvalidOperationException("body handler failure"), CancellationToken);
		}
		catch (InvalidOperationException)
		{
			threw = true;
		}

		threw.AssertTrue("the aborted write should have propagated the handler exception");
		bodyWriter.Stream.Position.AssertEqual(0L,
			"an aborted write must reset the reused body writer, else the next message ships prepended with the aborted body");
	}
}
