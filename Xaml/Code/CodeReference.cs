namespace StockSharp.Xaml.Code
{
	/// <summary>
	/// Ссылка на .NET сборку.
	/// </summary>
	public class CodeReference
	{
		/// <summary>
		/// Создать <see cref="CodeReference"/>.
		/// </summary>
		public CodeReference()
		{
		}

		/// <summary>
		/// Название сборки.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Путь к сборке.
		/// </summary>
		public string Location { get; set; }
	}
}