namespace StockSharp.Community
{
	/// <summary>
	/// Контент страницы документации.
	/// </summary>
	public class DocPageContent
	{
		/// <summary>
		/// Создать <see cref="DocPageContent"/>.
		/// </summary>
		public DocPageContent()
		{
		}

		/// <summary>
		/// Заголовок.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Тело.
		/// </summary>
		public string Body { get; set; }
	}

	/// <summary>
	/// Страница документации.
	/// </summary>
	public class DocPage
	{
		/// <summary>
		/// Создать <see cref="DocPage"/>.
		/// </summary>
		public DocPage()
		{
		}

		/// <summary>
		/// Контент на русском языке.
		/// </summary>
		public DocPageContent RussianContent { get; set; }

		/// <summary>
		/// Контент на английском языке.
		/// </summary>
		public DocPageContent EnglishContent { get; set; }

		/// <summary>
		/// Строка запроса.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Признак наличия дочерних страниц.
		/// </summary>
		public bool HasChild { get; set; }
	}
}