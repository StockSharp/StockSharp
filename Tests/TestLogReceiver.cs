namespace StockSharp.Tests;

internal abstract class TestLogReceiver : BaseLogReceiver
{
	protected TestLogReceiver()
	{
		Log += message => Logs.Add(message);
	}

	public List<LogMessage> Logs { get; } = [];

	public DateTime Time { get; set; } = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public override DateTime CurrentTime => Time;
}
