#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: DocPage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	/// <summary>
	/// The documentation page content.
	/// </summary>
	public class DocPageContent
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocPageContent"/>.
		/// </summary>
		public DocPageContent()
		{
		}

		/// <summary>
		/// Header.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Body.
		/// </summary>
		public string Body { get; set; }
	}

	/// <summary>
	/// Doc page.
	/// </summary>
	public class DocPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocPage"/>.
		/// </summary>
		public DocPage()
		{
		}

		/// <summary>
		/// Content in Russian.
		/// </summary>
		public DocPageContent RussianContent { get; set; }

		/// <summary>
		/// Content in English.
		/// </summary>
		public DocPageContent EnglishContent { get; set; }

		/// <summary>
		/// Url.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// The attribute of child pages presence .
		/// </summary>
		public bool HasChild { get; set; }
	}
}