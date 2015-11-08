namespace StockSharp.BusinessEntities
{
	/// <summary>
	/// The interface for access to provider of information about news.
	/// </summary>
	public interface INewsProvider
	{
		/// <summary>
		/// Request news <see cref="BusinessEntities.News.Story"/> body. After receiving the event <see cref="IConnector.NewsChanged"/> will be triggered.
		/// </summary>
		/// <param name="news">News.</param>
		void RequestNewsStory(News news);
	}
}