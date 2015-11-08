namespace SampleDiagram
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Xaml.Diagram;

	using ConfigurationExtensions = StockSharp.Configuration.Extensions;

	public enum ElementTypes
	{
		Composition,
		Strategy
	}

	public class StrategiesRegistry : BaseLogReceiver
	{
		private readonly string _compositionsPath;
		private readonly ObservableCollection<CompositionDiagramElement> _strategies = new ObservableCollection<CompositionDiagramElement>();
		private readonly ObservableCollection<CompositionDiagramElement> _compositions = new ObservableCollection<CompositionDiagramElement>();
		private readonly XmlSerializer<SettingsStorage> _serializer = new XmlSerializer<SettingsStorage>();

		private readonly CompositionRegistry _compositionRegistry;
		private readonly string _strategiesPath;

		public IEnumerable<CompositionDiagramElement> Strategies { get { return _strategies; } }

		public IEnumerable<CompositionDiagramElement> Compositions { get { return _compositions; } }

		public INotifyList<DiagramElement> DiagramElements { get { return _compositionRegistry.DiagramElements; } }

		public StrategiesRegistry(string compositionsPath = "Compositions", string strategiesPath = "Strategies")
		{
			if (compositionsPath == null)
				throw new ArgumentNullException("compositionsPath");
			
			if (strategiesPath == null)
				throw new ArgumentNullException("strategiesPath");

			_compositionRegistry = new CompositionRegistry();
			_compositionsPath = Path.GetFullPath(compositionsPath);
			_strategiesPath = Path.GetFullPath(strategiesPath);

			LoadElements();
			LoadStrategies();
		}

		public void Save(CompositionDiagramElement element, bool isComposition)
		{
			if (element == null)
				throw new ArgumentNullException("element");

			if (!isComposition)
			{
				if (!_strategies.Contains(element))
					_strategies.Add(element);
			}
			else
			{
				DiagramElements.Add(element);

				if (!_compositions.Contains(element))
					_compositions.Add(element);
			}

			var path = isComposition ? _compositionsPath : _strategiesPath;
			var settings = _compositionRegistry.Serialize(element);
			var file = Path.Combine(path, element.GetFileName());

			_serializer.Serialize(settings, file);
		}

		public void Remove(CompositionDiagramElement element, bool isComposition)
		{
			if (element == null)
				throw new ArgumentNullException("element");

			if (isComposition)
			{
				_compositionRegistry.TryRemove(element);
				_compositions.Remove(element);
			}
			else
				_strategies.Remove(element);

			var path = isComposition ? _compositionsPath : _strategiesPath;
			var file = Path.Combine(path, element.GetFileName());

			if (File.Exists(file))
				File.Delete(file);
		}

		public CompositionDiagramElement Discard(CompositionDiagramElement element, bool isComposition)
		{
			if (element == null)
				throw new ArgumentNullException("element");

			var path = isComposition ? _compositionsPath : _strategiesPath;
			var file = Path.Combine(path, element.GetFileName());
			var settings = _serializer.Deserialize(file);
			var discardedElement = _compositionRegistry.Deserialize(settings);

			if (isComposition)
			{
				// TODO discard in CompositionRegistry
				DiagramElements.Remove(element);
				DiagramElements.Add(discardedElement);

				var index = _compositions.IndexOf(element);
				_compositions[index] = discardedElement;
			}
			else
			{
				var index = _strategies.IndexOf(element);
				_strategies[index] = discardedElement;
			}

			return discardedElement;
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

			if (!Directory.Exists(_compositionsPath))
				Directory.CreateDirectory(_compositionsPath);

			var files = Directory.GetFiles(_compositionsPath, "*.xml");

			foreach (var file in files)
			{
				try
				{
					_compositionRegistry.Load(_serializer.Deserialize(file));
				}
				catch (Exception excp)
				{
					this.AddErrorLog("Load {0} composition element error: {1}", file, excp);
				}
			}

			_compositions.Clear();
			_compositions.AddRange(DiagramElements.OfType<CompositionDiagramElement>());
		}

		private void LoadStrategies()
		{
			if (!Directory.Exists(_strategiesPath))
				Directory.CreateDirectory(_strategiesPath);

			var files = Directory.GetFiles(_strategiesPath, "*.xml");

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