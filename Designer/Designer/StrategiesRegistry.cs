#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: StrategiesRegistry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Designer
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;
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
		private readonly SynchronizedList<DiagramElement> _strategies = new SynchronizedList<DiagramElement>();
		private readonly XmlSerializer<SettingsStorage> _serializer = new XmlSerializer<SettingsStorage>();

		private readonly CompositionRegistry _compositionRegistry;
		private readonly string _strategiesPath;

		public INotifyList<DiagramElement> Strategies => _strategies;

		public INotifyList<DiagramElement> Compositions => _compositionRegistry.DiagramElements;

		public INotifyList<DiagramElement> DiagramElements => _compositionRegistry.DiagramElements;

		public StrategiesRegistry(string compositionsPath = "Compositions", string strategiesPath = "Strategies")
		{
			if (compositionsPath == null)
				throw new ArgumentNullException(nameof(compositionsPath));
			
			if (strategiesPath == null)
				throw new ArgumentNullException(nameof(strategiesPath));

			_compositionRegistry = new CompositionRegistry();
			_compositionsPath = Path.GetFullPath(compositionsPath);
			_strategiesPath = Path.GetFullPath(strategiesPath);
		}

		public void Init()
		{
			LoadElements();
			LoadStrategies();
		}

		public void Save(CompositionItem element)
		{
			Save(element.Element, element.Type == CompositionType.Composition);
		}

		public void Save(CompositionDiagramElement element, bool isComposition)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (!isComposition)
			{
				if (!_strategies.Contains(element))
					_strategies.Add(element);
			}
			else
				DiagramElements.Add(element);

			var path = isComposition ? _compositionsPath : _strategiesPath;
			var settings = _compositionRegistry.Serialize(element);
			var file = Path.Combine(path, element.GetFileName());

			_serializer.Serialize(settings, file);
		}

		public void Remove(CompositionItem element)
		{
			Remove(element.Element, element.Type == CompositionType.Composition);
		}

		public void Remove(CompositionDiagramElement element, bool isComposition)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (isComposition)
			{
				_compositionRegistry.TryRemove(element);
			}
			else
				_strategies.Remove(element);

			var path = isComposition ? _compositionsPath : _strategiesPath;
			var file = Path.Combine(path, element.GetFileName());

			if (File.Exists(file))
				File.Delete(file);
		}

		public void Discard(CompositionItem element)
		{
			Discard(element.Element, element.Type == CompositionType.Composition);
		}

		public void Discard(CompositionDiagramElement element, bool isComposition)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			var path = isComposition ? _compositionsPath : _strategiesPath;
			var file = Path.Combine(path, element.GetFileName());
			var settings = _serializer.Deserialize(file);

			_compositionRegistry.Load(element, settings);
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
			var compositions = GetSchemas("composition_");

			foreach (var file in files)
			{
				try
				{
					var settings = _serializer.Deserialize(file);
					var element = _compositionRegistry.Deserialize(settings);

					_compositionRegistry.DiagramElements.Add(element);
					compositions.Remove(element.TypeId);
				}
				catch (Exception excp)
				{
					this.AddErrorLog(LocalizedStrings.Str3046Params, file, excp);
				}
			}

			foreach (var pair in compositions)
			{
				try
				{
					var settings = _serializer.Deserialize(pair.Value.To<Stream>());
					var element = _compositionRegistry.Deserialize(settings);

					Save(element, true);
				}
				catch (Exception excp)
				{
					this.AddErrorLog(LocalizedStrings.Str3046Params, pair.Key, excp);
				}
			}
		}

		private void LoadStrategies()
		{
			if (!Directory.Exists(_strategiesPath))
				Directory.CreateDirectory(_strategiesPath);

			var files = Directory.GetFiles(_strategiesPath, "*.xml");
			var strategies = GetSchemas("strategy_");

			foreach (var file in files)
			{
				try
				{
					var settings = _serializer.Deserialize(file);
					var element = _compositionRegistry.Deserialize(settings);

					_strategies.Add(element);
					strategies.Remove(element.TypeId);
				}
				catch (Exception excp)
				{
					this.AddErrorLog(LocalizedStrings.Str3627Params, file, excp);
				}
			}

			foreach (var pair in strategies)
			{
				try
				{
					var settings = _serializer.Deserialize(pair.Value.To<Stream>());
					var element = _compositionRegistry.Deserialize(settings);

					Save(element, false);
				}
				catch (Exception excp)
				{
					this.AddErrorLog(LocalizedStrings.Str3627Params, pair.Key, excp);
				}
			}
		}

		private static IDictionary<Guid, string> GetSchemas(string prefix)
		{
			return Properties
				.Resources
				.ResourceManager
				.GetResourceSet(CultureInfo.CurrentCulture, true, true)
				.Cast<System.Collections.DictionaryEntry>()
				.Where(i => i.Key.ToString().StartsWith(prefix))
				.ToDictionary(pair => ((string)pair.Key).TrimStart(prefix).Replace("_", "-").To<Guid>(), pair => (string)pair.Value);
		}
	}
}