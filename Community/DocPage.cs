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