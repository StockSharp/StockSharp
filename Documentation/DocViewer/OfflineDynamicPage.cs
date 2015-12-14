#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.DocViewer.DocViewer
File: OfflineDynamicPage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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