namespace StockSharp.FTX.Native.Model;

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class FtxRestResponse<T>
	where T : class
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("result")]
	public T Result { get; set; }
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class FtxRestResponseHasMoreData<T> : FtxRestResponse<T>
	where T : class
{
	[JsonProperty("hasMoreData")]
	public bool HasMoreData { get; set; }
}