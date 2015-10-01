namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Online doc url attribute.
	/// </summary>
	public class DocAttribute : Attribute
	{
		/// <summary>
		/// Online doc url.
		/// </summary>
		public string DocUrl { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DocAttribute"/>.
		/// </summary>
		/// <param name="docUrl">Online doc url.</param>
		public DocAttribute(string docUrl)
		{
			if (docUrl.IsEmpty())
				throw new ArgumentNullException("docUrl");

			DocUrl = docUrl;
		}
	}
}