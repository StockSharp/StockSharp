namespace StockSharp.DarkHorse.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class DarkHorseRestResponse<T>
	where T : class
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("result")]
	public T Result { get; set; }
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class DarkHorseRestResponseHasMoreData<T> : DarkHorseRestResponse<T>
	where T : class
{
	[JsonProperty("hasMoreData")]
	public bool HasMoreData { get; set; }
}