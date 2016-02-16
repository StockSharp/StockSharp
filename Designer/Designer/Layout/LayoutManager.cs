#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.Layout.SampleDiagramPublic
File: LayoutManager.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Designer.Layout
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Studio.Controls;

	using Xceed.Wpf.AvalonDock;
	using Xceed.Wpf.AvalonDock.Layout;
	using Xceed.Wpf.AvalonDock.Layout.Serialization;

	public sealed class LayoutManager : BaseLogReceiver
	{
		private readonly Dictionary<string, LayoutDocument> _documents = new Dictionary<string, LayoutDocument>();
		private readonly Dictionary<string, LayoutAnchorable> _anchorables = new Dictionary<string, LayoutAnchorable>();

		private readonly SynchronizedDictionary<BaseStudioControl, SettingsStorage> _dockingControlSettings = new SynchronizedDictionary<BaseStudioControl, SettingsStorage>();
		private readonly SynchronizedSet<BaseStudioControl> _changedControls = new CachedSynchronizedSet<BaseStudioControl>();

		private readonly TimeSpan _period = TimeSpan.FromSeconds(5);
		private readonly object _syncRoot = new object();

		private Timer _flushTimer;
		private bool _isFlushing;
		private bool _isLayoutChanged;
		private bool _isDisposing;
		private bool _isSettingsChanged;
		private string _layout;

		private IEnumerable<LayoutDocumentPane> TabGroups => DockingManager.Layout.Descendents().OfType<LayoutDocumentPane>().ToArray();

		private LayoutPanel RootGroup => DockingManager.Layout.RootPanel;

		public DockingManager DockingManager { get; }

		public IEnumerable<BaseStudioControl> DockingControls
		{
			get
			{
				return DockingManager
					.Layout
					.Descendents()
					.OfType<LayoutContent>()
					.Select(c => c.Content)
					.OfType<BaseStudioControl>()
					.ToArray();
			}
		}

		public event Action Changed; 

		public LayoutManager(DockingManager dockingManager)
		{
			if (dockingManager == null)
				throw new ArgumentNullException(nameof(dockingManager));

			DockingManager = dockingManager;
			DockingManager.LayoutChanged += OnDockingManagerLayoutChanged;
			DockingManager.DocumentClosing += OnDockingManagerDocumentClosing;
			DockingManager.DocumentClosed += OnDockingManagerDocumentClosed;

			OnDockingManagerLayoutChanged(null, null);
		}

		public void OpenToolWindow(string key, string title, object content, bool canClose = true)
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
					ContentId = key,
					Title = title,
					Content = content,
					CanClose = canClose
				};

				_anchorables.Add(key, anchorable);
				RootGroup.Children.Add(new LayoutAnchorablePane(anchorable));
			}

			DockingManager.ActiveContent = anchorable.Content;
		}

		public void OpenToolWindow(BaseStudioControl content, bool canClose = true)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var anchorable = _anchorables.TryGetValue(content.Key);

			if (anchorable == null)
			{
				content.Changed += OnBaseStudioControlChanged;

				anchorable = new LayoutAnchorable
				{
					ContentId = content.Key,
					Content = content,
					CanClose = canClose
				};

				anchorable.SetBindings(LayoutContent.TitleProperty, content, "Title");

				_anchorables.Add(content.Key, anchorable);
			
				RootGroup.Children.Add(new LayoutAnchorablePane(anchorable));
				OnBaseStudioControlChanged(content);
			}

			DockingManager.ActiveContent = anchorable.Content;
		}

		public void OpenDocumentWindow(string key, string title, object content, bool canClose = true)
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

		public void OpenDocumentWindow(BaseStudioControl content, bool canClose = true)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var document = _documents.TryGetValue(content.Key);

			if (document == null)
			{
				content.Changed += OnBaseStudioControlChanged;

				document = new LayoutDocument
				{
					ContentId = content.Key,
					Content = content,
					CanClose = canClose
				};

				document.SetBindings(LayoutContent.TitleProperty, content, "Title");

				_documents.Add(content.Key, document);

				TabGroups.First().Children.Add(document);
				OnBaseStudioControlChanged(content);
			}

			DockingManager.ActiveContent = document.Content;
		}

		public void CloseDocumentWindow(BaseStudioControl content)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var document = _documents.TryGetValue(content.Key);

			if (document == null)
				return;

			document.Close();
		}

		public override void Load(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			_documents.Clear();
			_anchorables.Clear();
			_changedControls.Clear();
			_dockingControlSettings.Clear();

			var controls = storage.GetValue<SettingsStorage[]>("Controls");

			foreach (var settings in controls)
			{
				try
				{
					var control = LoadBaseStudioControl(settings);

					_dockingControlSettings.Add(control, settings);
					OpenDocumentWindow(control);
				}
				catch (Exception excp)
				{
					this.AddErrorLog(excp);
				}
			}

			_layout = storage.GetValue<string>("Layout");

			if (!_layout.IsEmpty())
				LoadLayout(_layout);
		}

		public override void Save(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			storage.SetValue("Controls", _dockingControlSettings.SyncGet(c => c.Select(p => p.Value).ToArray()));
			storage.SetValue("Layout", _layout);
		}

		public void LoadLayout(string settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			try
			{
				var titles = DockingManager
					.Layout
					.Descendents()
					.OfType<LayoutContent>()
					.ToDictionary(c => c.ContentId, c => c.Title);

				using (var reader = new StringReader(settings))
				{
					var layoutSerializer = new XmlLayoutSerializer(DockingManager);
					layoutSerializer.LayoutSerializationCallback += (s, e) =>
					{
						if (e.Content == null)
							e.Model.Close();
					};
					layoutSerializer.Deserialize(reader);
				}

				var items = DockingManager
					.Layout
					.Descendents()
					.OfType<LayoutContent>();

				foreach (var content in items.Where(c => c.Content is BaseStudioControl))
				{
					content.DoIfElse<LayoutDocument>(d => _documents[d.ContentId] = d, () => { });
					content.DoIfElse<LayoutAnchorable>(d => _anchorables[d.ContentId] = d, () => { });

					if (!(content.Content is BaseStudioControl))
					{
						var title = titles.TryGetValue(content.ContentId);

						if (!title.IsEmpty())
							content.Title = title;
					}
					else
						content.SetBindings(LayoutContent.TitleProperty, content.Content, "Title");
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, LocalizedStrings.Str3649);
			}
		}

		public string SaveLayout()
		{
			var builder = new StringBuilder();

			try
			{
				using (var writer = new StringWriter(builder))
				{
					new XmlLayoutSerializer(DockingManager).Serialize(writer);
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, LocalizedStrings.Str3649);
			}

			return builder.ToString();
		}

		public void FlushSettings()
		{
			_isSettingsChanged = true;
			Flush();
		}

		protected override void DisposeManaged()
		{
			_isDisposing = true;

			Save(DockingControls, true);
			Changed.SafeInvoke();

			base.DisposeManaged();
		}

		private void OnDockingManagerLayoutChanged(object sender, EventArgs e)
		{
			if (DockingManager.Layout == null)
				return;

			DockingManager.Layout.Updated += OnLayoutUpdated;
		}

		private void OnLayoutUpdated(object sender, EventArgs e)
		{
			_isLayoutChanged = true;
			Flush();
		}

		private void OnDockingManagerDocumentClosing(object sender, DocumentClosingEventArgs e)
		{
			var control = e.Document.Content as BaseStudioControl;

			if (control == null)
				return;

			e.Cancel = !control.CanClose();
		}

		private void OnDockingManagerDocumentClosed(object sender, DocumentClosedEventArgs e)
		{
			var control = e.Document.Content as BaseStudioControl;

			if (control == null)
				return;

			_documents.RemoveWhere(p => Equals(p.Value, e.Document));

			_isLayoutChanged = true;

			_changedControls.Remove(control);
			_dockingControlSettings.Remove(control);

			Flush();
		}

		private void OnBaseStudioControlChanged(BaseStudioControl control)
		{
			_changedControls.Add(control);
			Flush();
		}

		private void Flush()
		{
			lock (_syncRoot)
			{
				if (_isFlushing || _flushTimer != null)
					return;

				_flushTimer = new Timer(OnFlush, null, _period, _period);
			}
		}

		private void OnFlush(object state)
		{
			BaseStudioControl[] items;
			bool isLayoutChanged;
			bool isSettingsChanged;

			lock (_syncRoot)
			{
				if (_isFlushing || _isDisposing)
					return;

				isLayoutChanged = _isLayoutChanged;
				isSettingsChanged = _isSettingsChanged;
				items = _changedControls.CopyAndClear();

				_isFlushing = true;
				_isLayoutChanged = false;
				_isSettingsChanged = false;
			}

			try
			{
				var needSave = items.Length > 0 || isLayoutChanged;

                if (needSave || isSettingsChanged)
				{
					if (needSave)
					{
						GuiDispatcher.GlobalDispatcher.AddSyncAction(() =>
						{
							if (_isDisposing)
								return;

							Save(items, isLayoutChanged);
						});
					}

					Changed.SafeInvoke();
				}
				else
				{
					lock (_syncRoot)
					{
						if (_flushTimer == null)
							return;

						_flushTimer.Dispose();
						_flushTimer = null;
					}
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, "Flush layout changed error.");
			}
			finally
			{
				lock (_syncRoot)
					_isFlushing = false;
			}
		}

		private void Save(IEnumerable<BaseStudioControl> items, bool isLayoutChanged)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				foreach (var control in items)
				{
					var storage = new SettingsStorage();
					storage.SetValue("ControlType", control.GetType().GetTypeName(false));
					control.Save(storage);

					_dockingControlSettings[control] = storage;
				}

				if (isLayoutChanged)
					_layout = SaveLayout();
			});
		}

		private static BaseStudioControl LoadBaseStudioControl(SettingsStorage settings)
		{
			var type = settings.GetValue<Type>("ControlType");
			var control = (BaseStudioControl)Activator.CreateInstance(type);

			control.Load(settings);

			return control;
		}
	}
}
