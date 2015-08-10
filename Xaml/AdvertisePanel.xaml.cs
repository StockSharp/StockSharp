namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Threading;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Localization;
	using Ecng.Web.BBCodes;

	using StockSharp.Community;
	using StockSharp.Localization;

	/// <summary>
	/// Панель отображения рекламных акций.
	/// </summary>
	public partial class AdvertisePanel
	{
		private readonly NotificationClient _client;
		private readonly List<CommunityNews> _news = new List<CommunityNews>();
		private int _index = -1;
		private readonly BBCodeParser _parser;
		private DispatcherTimer _timer;

		/// <summary>
		/// Создать <see cref="AdvertisePanel"/>.
		/// </summary>
		public AdvertisePanel()
		{
			InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			_client = ConfigManager.TryGetService<NotificationClient>() ?? new NotificationClient();
			_client.NewsReceived += OnNewsReceived;

			var sizes = new[] { 10, 20, 30, 40, 50, 60, 70, 110, 120 };

			_parser = new BBCodeParser(new[]
			{
				new BBTag("b", "<b>", "</b>"),
				new BBTag("i", "<span style=\"font-style:italic;\">", "</span>"),
				new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>"),
				new BBTag("center", "<div style=\"align:center;\">", "</div>"),
				new BBTag("size", "<span style=\"font-size:${fontSize}%;\">", "</div>", new BBAttribute("fontSize", "", c => sizes[c.AttributeValue.To<int>()].To<string>())),
				new BBTag("code", "<pre class=\"prettyprint\">", "</pre>"),
				new BBTag("img", "<img src=\"${content}\" />", "", false, true),
				new BBTag("quote", "<blockquote>", "</blockquote>"),
				new BBTag("list", "<ul>", "</ul>"),
				new BBTag("*", "<li>", "</li>", true, false),
				new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
			});

			_timer = new DispatcherTimer();
			_timer.Tick += OnTick;
			_timer.Interval = new TimeSpan(0, 1, 0);
			_timer.Start();
		}

		private void OnTick(object sender, EventArgs e)
		{
			OnNextClick(sender, null);
		}

		private static string GetContent(CommunityNews news)
		{
			var isRu = LocalizedStrings.ActiveLanguage == Languages.Russian;
			return isRu ? news.RussianBody : news.EnglishBody;
		}

		private void OnNewsReceived(CommunityNews news)
		{
			var now = DateTime.UtcNow;

			_news.RemoveWhere(n => n.EndDate <= now);

			if (GetContent(news).IsEmpty())
				return;

			_news.Add(news);

			_index = 0;
			ShowNews();
		}

		private void ShowNews()
		{
			var news = _news[_index];

			var content = GetContent(news);

			if (content.IsEmpty())
				return;

			HtmlPanel.Text = _parser.ToHtml(content);
		}

		private void AdvertisePanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			_client.SubscribeNews();
		}

		private void OnNextClick(object sender, RoutedEventArgs e)
		{
			if (_news.Count <= 0 || _index >= (_news.Count - 1))
				return;

			_index++;
			ShowNews();
		}

		private void OnPrevClick(object sender, RoutedEventArgs e)
		{
			if (_news.Count <= 0 || _index < 1)
				return;

			_index--;
			ShowNews();
		}
	}
}