#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: AdvertisePanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Threading;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Localization;
	using Ecng.Web.BBCodes;
	using Ecng.Xaml;

	using StockSharp.Community;
	using StockSharp.Localization;

	/// <summary>
	/// Panel for advertising action displaying.
	/// </summary>
	public partial class AdvertisePanel
	{
		private readonly SynchronizedList<CommunityNews> _news = new SynchronizedList<CommunityNews>();
		private int _index = -1;
		private readonly BBCodeParser _parser;
		private DispatcherTimer _timer;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdvertisePanel"/>.
		/// </summary>
		public AdvertisePanel()
		{
			InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

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

		private NotificationClient _client;

		/// <summary>
		/// The client for access to the StockSharp notification service.
		/// </summary>
		public NotificationClient Client
		{
			get { return _client; }
			set
			{
				if (_client == value)
					return;

				if (_client != null)
				{
					_client.NewsReceived -= OnNewsReceived;
					_client.UnSubscribeNews();
				}

				_client = value;

				if (_client != null)
				{
					_client.NewsReceived += OnNewsReceived;
					_client.SubscribeNews();
				}
			}
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

			lock (_news.SyncRoot)
			{
				_news.RemoveWhere(n => n.EndDate <= now);

				if (GetContent(news).IsEmpty())
					return;

				_news.Add(news);
				_index = 0;
			}

			this.GuiAsync(() => ShowNews(news));
		}

		private void ShowNews(CommunityNews news)
		{
			HtmlPanel.Text = _parser.ToHtml(GetContent(news));
		}

		//private void AdvertisePanel_OnLoaded(object sender, RoutedEventArgs e)
		//{
		//	if (DesignerProperties.GetIsInDesignMode(this))
		//		return;

		//	try
		//	{
		//		_client.SubscribeNews();
		//	}
		//	catch (Exception ex)
		//	{
		//		ex.LogError();
		//	}
		//}

		private void OnNextClick(object sender, RoutedEventArgs e)
		{
			CommunityNews news;

			lock (_news.SyncRoot)
			{
				if (_news.Count == 0)
					return;

				if (_index >= (_news.Count - 1))
					_index = 0;
				else
					_index++;

				news = _news[_index];
			}

			ShowNews(news);
		}

		private void OnPrevClick(object sender, RoutedEventArgs e)
		{
			CommunityNews news;

			lock (_news.SyncRoot)
			{
				if (_news.Count == 0)
					return;

				if (_index < 1)
					_index = _news.Count - 1;
				else
					_index--;

				news = _news[_index];
			}

			ShowNews(news);
		}
	}
}