namespace StockSharp.DocViewer
{
	using System.Collections.Generic;

	public class OfflineDynamicPage
	{
		public string UrlPart { get; set; }
		public OfflineDynamicPage Parent { get; set; }
		public IList<OfflineDynamicPage> Childs = new List<OfflineDynamicPage>();
		public string RussianTitle { get; set; }
		public string RussianContent { get; set; }
	}
}