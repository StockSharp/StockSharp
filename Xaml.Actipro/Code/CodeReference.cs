namespace StockSharp.Xaml.Actipro.Code
{
	/// <summary>
	/// The link to the .NET build.
	/// </summary>
	public class CodeReference
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CodeReference"/>.
		/// </summary>
		public CodeReference()
		{
		}

		/// <summary>
		/// The build name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The path to the build.
		/// </summary>
		public string Location { get; set; }
	}
}