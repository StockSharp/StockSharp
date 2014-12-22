namespace SampleSmart
{
	public class SampleQuote
	{
		public string Bid { get; set; }
		public string Ask { get; set; }
		public double Price { get; set; }
		public bool HasVolume { get { return this.OwnVolume > 0; } }
		public int OwnVolume { get; set; }
	}
}