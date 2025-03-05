namespace StockSharp.BusinessEntities;

/// <summary>
/// The interface for access to provider of information about news.
/// </summary>
public interface INewsProvider
{
	/// <summary>
	/// Request news <see cref="News.Story"/> body. After receiving the event <see cref="ISubscriptionProvider.NewsReceived"/> will be triggered.
	/// </summary>
	/// <param name="news">News.</param>
	/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
	void RequestNewsStory(News news, IMessageAdapter adapter = null);
}