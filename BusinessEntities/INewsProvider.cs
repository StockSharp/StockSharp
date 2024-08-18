namespace StockSharp.BusinessEntities;

using StockSharp.Messages;

/// <summary>
/// The interface for access to provider of information about news.
/// </summary>
public interface INewsProvider
{
	/// <summary>
	/// Request news <see cref="News.Story"/> body. After receiving the event <see cref="IMarketDataProvider.NewsChanged"/> will be triggered.
	/// </summary>
	/// <param name="news">News.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	void RequestNewsStory(News news, IMessageAdapter adapter = null);
}