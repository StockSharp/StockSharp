namespace SampleDiagram.Layout
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Logging;

	using Xceed.Wpf.AvalonDock;
	using Xceed.Wpf.AvalonDock.Layout;
	using Xceed.Wpf.AvalonDock.Layout.Serialization;

	public class LayoutManager : BaseLogReceiver
	{
		private readonly Dictionary<object, LayoutDocument> _documents = new Dictionary<object, LayoutDocument>();
		private readonly Dictionary<object, LayoutAnchorable> _anchorables = new Dictionary<object, LayoutAnchorable>();

		private IEnumerable<LayoutDocumentPane> TabGroups => DockingManager.Layout.Descendents().OfType<LayoutDocumentPane>().ToArray();

		private LayoutPanel RootGroup => DockingManager.Layout.RootPanel;

		public DockingManager DockingManager { get; }

		public IEnumerable<DockingControl> DockingControls
		{
			get
			{
				var documents = _documents
					.Select(d => d.Value.Content)
					.OfType<DockingControl>();

				var anchorables = _anchorables
					.Select(d => d.Value.Content)
					.OfType<DockingControl>();

				return documents.Concat(anchorables).ToArray();
			}
		}

		public LayoutManager(DockingManager dockingManager)
		{
			if (dockingManager == null)
				throw new ArgumentNullException(nameof(dockingManager));

			DockingManager = dockingManager;
		}

		public void OpenToolWindow(object key, string title, object content, bool canClose = true)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			if (title == null)
				throw new ArgumentNullException(nameof(title));

			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var anchorable = _anchorables.TryGetValue(key);

			if (anchorable == null)
			{
				anchorable = new LayoutAnchorable
				{
					ContentId = key.ToString(),
					Title = title,
					Content = content,
					CanClose = canClose
				};

				_anchorables.Add(key, anchorable);
				RootGroup.Children.Add(new LayoutAnchorablePane(anchorable));
			}

			DockingManager.ActiveContent = anchorable.Content;
		}

		public void OpenToolWindow(DockingControl content, bool canClose = true)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var anchorable = _anchorables.TryGetValue(content.Key);

			if (anchorable == null)
			{
				anchorable = new LayoutAnchorable
				{
					ContentId = content.Key.ToString(),
					Content = content,
					CanClose = canClose
				};

				anchorable.SetBindings(LayoutContent.TitleProperty, content, "Title");

				_anchorables.Add(content.Key, anchorable);
				RootGroup.Children.Add(new LayoutAnchorablePane(anchorable));
			}

			DockingManager.ActiveContent = anchorable.Content;
		}

		public void OpenDocumentWindow(object key, string title, object content, bool canClose = true)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			if (title == null)
				throw new ArgumentNullException(nameof(title));

			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var document = _documents.TryGetValue(key);

			if (document == null)
			{
				document = new LayoutDocument
				{
					ContentId = key.ToString(),
					Title = title,
					Content = content,
					CanClose = canClose
				};

				_documents.Add(key, document);
				TabGroups.First().Children.Add(document);
			}

			DockingManager.ActiveContent = document.Content;
		}

		public void OpenDocumentWindow(DockingControl content, bool canClose = true)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var document = _documents.TryGetValue(content.Key);

			if (document == null)
			{
				document = new LayoutDocument
				{
					ContentId = content.Key.ToString(),
					Content = content,
					CanClose = canClose
				};

				document.SetBindings(LayoutContent.TitleProperty, content, "Title");

				_documents.Add(content.Key, document);
				TabGroups.First().Children.Add(document);
			}

			DockingManager.ActiveContent = document.Content;
		}

		public void Load(string settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			try
			{
				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					using (var reader = new StringReader(settings))
					{
						var layoutSerializer = new XmlLayoutSerializer(DockingManager);
						layoutSerializer.LayoutSerializationCallback += (s, e) =>
						{
							//if (e.Content == null)
							//	e.Model.Close();
						};
						layoutSerializer.Deserialize(reader);
					}
				});
			}
			catch (Exception excp)
			{
				this.AddErrorLog("Ошибка загрузки разметки: {0}", excp);
			}
		}

		public string Save()
		{
			var builder = new StringBuilder();

			try
			{
				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					using (var writer = new StringWriter(builder))
					{
						new XmlLayoutSerializer(DockingManager).Serialize(writer);
					}
				});
			}
			catch (Exception excp)
			{
				this.AddErrorLog("Ошибка сохранения разметки: {0}", excp);
			}

			return builder.ToString();
		}
	}
}
