namespace StockSharp.Community
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// The client for access to the service of work with files and documents.
	/// </summary>
	public class FileClient : BaseCommunityClient<IFileService>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FileClient"/>.
		/// </summary>
		public FileClient()
			: this("http://stocksharp.com/services/fileservice.svc".To<Uri>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileClient"/>.
		/// </summary>
		/// <param name="address">Service address.</param>
		public FileClient(Uri address)
			: base(address, "file")
		{
		}

		/// <summary>
		/// To upload the file to the site .
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="body">File body.</param>
		/// <returns>Uploaded file link.</returns>
		public string Upload(string fileName, byte[] body)
		{
			return Invoke(f => f.Upload(SessionId, fileName, body));
		}
	}
}