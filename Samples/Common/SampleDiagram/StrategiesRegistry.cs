namespace SampleDiagram
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.IO;

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Xaml.Diagram;

	using ConfigurationExtensions = StockSharp.Configuration.Extensions;

	public class StrategiesRegistry : BaseLogReceiver
	{
		private readonly ObservableCollection<CompositionDiagramElement> _strategies = new ObservableCollection<CompositionDiagramElement>();
		private readonly XmlSerializer<SettingsStorage> _serializer = new XmlSerializer<SettingsStorage>();

		private readonly CompositionRegistry _compositionRegistry;
		private readonly string _path;

		public IEnumerable<CompositionDiagramElement> Strategies { get { return _strategies; } }

		public INotifyList<DiagramElement> DiagramElements { get { return _compositionRegistry.DiagramElements; } }

		public StrategiesRegistry(string path = "Strategies")
		{
			if (path == null)
				throw new ArgumentNullException("path");

			_compositionRegistry = new CompositionRegistry();
			_path = Path.GetFullPath(path);

			LoadElements();
			LoadStrategies();
		}

		public void Save(CompositionDiagramElement element)
		{
			if (element == null)
				throw new ArgumentNullException("element");

			if (!_strategies.Contains(element))
				_strategies.Add(element);

			var settings = _compositionRegistry.Serialize(element);
			var file = Path.Combine(_path, element.GetFileName());

			_serializer.Serialize(settings, file);
		}

		public CompositionDiagramElement Discard(CompositionDiagramElement element)
		{
			if (element == null)
				throw new ArgumentNullException("element");

			var file = Path.Combine(_path, element.GetFileName());
			var settings = _serializer.Deserialize(file);
			var discardedElement = _compositionRegistry.Deserialize(settings);

			var index = _strategies.IndexOf(element);
			_strategies[index] = discardedElement;

			return discardedElement;
		}

		public void Remove(CompositionDiagramElement element)
		{
			if (element == null)
				throw new ArgumentNullException("element");

			_strategies.Remove(element);

			var file = Path.Combine(_path, element.GetFileName());
			
			if (File.Exists(file))
				File.Delete(file);
		}

		public CompositionDiagramElement Clone(CompositionDiagramElement element)
		{
			var settings = _compositionRegistry.Serialize(element);
			var clone = _compositionRegistry.Deserialize(settings);

			return clone;
		}

		private void LoadElements()
		{
			foreach (var element in ConfigurationExtensions.GetDiagramElements())
				_compositionRegistry.DiagramElements.Add(element);
		}

		private void LoadStrategies()
		{
			if (!Directory.Exists(_path))
				Directory.CreateDirectory(_path);

			var files = Directory.GetFiles(_path, "*.xml");

			foreach (var file in files)
			{
				try
				{
					var settings = _serializer.Deserialize(file);
					var element = _compositionRegistry.Deserialize(settings);

					_strategies.Add(element);
				}
				catch (Exception excp)
				{
					this.AddErrorLog("Load {0} strategy error: {1}", file, excp);
				}
			}
		}
	}
}