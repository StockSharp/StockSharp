namespace StockSharp.Tests;

using StockSharp.Fix;

/// <summary>
/// Round-trip conversion tests for MessageConverter.
/// Tests: Message -> FixXxx -> Message, then compare with original.
/// </summary>
[TestClass]
public partial class MessageConverterTests : BaseTestClass
{
	protected IMessageConverter Converter { get; } = new MessageConverter();

	protected static SecurityId CreateTestSecurityId(string code = "AAPL", string board = "NYSE")
		=> new() { SecurityCode = code, BoardCode = board };

	protected static DateTime CreateTestDateTime()
		=> new(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
}
