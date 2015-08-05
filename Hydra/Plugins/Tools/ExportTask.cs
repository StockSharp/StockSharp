namespace StockSharp.Hydra.Tools
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data.Providers;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.Xaml.Database;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Export;
	using StockSharp.Algo.Storages;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Xaml.PropertyGrid;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(LocalizedStrings.Str3754Key)]
	[DescriptionLoc(LocalizedStrings.Str3767Key)]
	[TaskDoc("http://stocksharp.com/doc/html/9e075b32-abb2-4fad-bfb2-b822dd7d9f30.htm")]
	[TaskIcon("export_logo.png")]
	[TaskCategory(TaskCategories.Tool)]
	class ExportTask : BaseHydraTask
	{
		[TaskSettingsDisplayName(LocalizedStrings.Str3754Key)]
		[CategoryOrderLoc(LocalizedStrings.Str3754Key, 0)]
		[CategoryOrderLoc(LocalizedStrings.CandlesKey, 1)]
		[CategoryOrder("CSV", 2)]
		[CategoryOrderLoc(LocalizedStrings.Str3755Key, 3)]
		[CategoryOrderLoc(LocalizedStrings.GeneralKey, 4)]
		private sealed class ExportSettings : HydraTaskSettings
		{
			public ExportSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(LocalizedStrings.Str3754Key)]
			[DisplayNameLoc(LocalizedStrings.TypeKey)]
			[DescriptionLoc(LocalizedStrings.Str3756Key)]
			[PropertyOrder(0)]
			public ExportTypes ExportType
			{
				get { return ExtensionInfo["ExportType"].To<ExportTypes>(); }
				set { ExtensionInfo["ExportType"] = value.To<string>(); }
			}

			[CategoryLoc(LocalizedStrings.Str3754Key)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str3757Key)]
			[PropertyOrder(1)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(LocalizedStrings.Str3754Key)]
			[DisplayNameLoc(LocalizedStrings.Str3758Key)]
			[DescriptionLoc(LocalizedStrings.Str3759Key)]
			[PropertyOrder(2)]
			[Editor(typeof(FolderBrowserEditor), typeof(FolderBrowserEditor))]
			public string ExportFolder
			{
				get { return (string)ExtensionInfo["ExportFolder"]; }
				set { ExtensionInfo["ExportFolder"] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3754Key)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str3760Key)]
			[PropertyOrder(2)]
			public int Offset
			{
				get { return ExtensionInfo["Offset"].To<int>(); }
				set { ExtensionInfo["Offset"] = value; }
			}

			[CategoryLoc(LocalizedStrings.CandlesKey)]
			[DisplayNameLoc(LocalizedStrings.Str225Key)]
			[DescriptionLoc(LocalizedStrings.Str3761Key)]
			[PropertyOrder(3)]
			[Editor(typeof(CandleSettingsEditor), typeof(CandleSettingsEditor))]
			public CandleSeries CandleSettings
			{
				get { return (CandleSeries)ExtensionInfo["CandleSettings"]; }
				set { ExtensionInfo["CandleSettings"] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3755Key)]
			[DisplayNameLoc(LocalizedStrings.Str174Key)]
			[DescriptionLoc(LocalizedStrings.Str3762Key)]
			[PropertyOrder(0)]
			[Editor(typeof(DatabaseConnectionEditor), typeof(DatabaseConnectionEditor))]
			public DatabaseConnectionPair Connection
			{
				get
				{
					var provider = (string)ExtensionInfo.TryGetValue("ConnectionProvider");
					var conStr = (string)ExtensionInfo.TryGetValue("ConnectionString");

					if (provider == null || conStr == null)
						return null;

					var type = provider.To<Type>();
					return DatabaseConnectionCache.Instance.GetConnection(DatabaseProviderRegistry.Providers.First(p => p.GetType() == type), conStr);
				}
				set
				{
					if (value == null)
					{
						ExtensionInfo.Remove("ConnectionProvider");
						ExtensionInfo.Remove("ConnectionString");
					}
					else
					{
						ExtensionInfo["ConnectionProvider"] = value.Provider.GetType().AssemblyQualifiedName;
						ExtensionInfo["ConnectionString"] = value.ConnectionString;
					}
				}
			}

			[CategoryLoc(LocalizedStrings.Str3755Key)]
			[DisplayNameLoc(LocalizedStrings.Str3763Key)]
			[DescriptionLoc(LocalizedStrings.Str3764Key)]
			[PropertyOrder(1)]
			public int BatchSize
			{
				get { return (int)ExtensionInfo["BatchSize"]; }
				set { ExtensionInfo["BatchSize"] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3755Key)]
			[DisplayNameLoc(LocalizedStrings.Str3765Key)]
			[DescriptionLoc(LocalizedStrings.Str3766Key)]
			[PropertyOrder(2)]
			public bool CheckUnique
			{
				get { return (bool)ExtensionInfo["CheckUnique"]; }
				set { ExtensionInfo["CheckUnique"] = value; }
			}

			[Category("CSV")]
			[DisplayName(LocalizedStrings.TemplateKey)]
			[DescriptionLoc(LocalizedStrings.TemplateKey, true)]
			[ExpandableObject]
			public TemplateTxtRegistry TemplateTxtRegistry
			{
				get { return (TemplateTxtRegistry)ExtensionInfo["TemplateTxtRegistry"]; }
				set { ExtensionInfo["TemplateTxtRegistry"] = value; }
			}

			public override HydraTaskSettings Clone()
			{
				var clone = (ExportSettings)base.Clone();
				clone.CandleSettings = CandleSettings.Clone();
				clone.TemplateTxtRegistry = TemplateTxtRegistry.Clone();
				return clone;
			}
		}

		private ExportSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new ExportSettings(settings);

			if (settings.IsDefault)
			{
				_settings.ExportType = ExportTypes.Txt;
				_settings.Offset = 1;
				_settings.ExportFolder = string.Empty;
				_settings.CandleSettings = new CandleSeries { CandleType = typeof(TimeFrameCandle), Arg = TimeSpan.FromMinutes(1) };
				//_settings.ExportTemplate = "{OpenTime:yyyy-MM-dd HH:mm:ss};{OpenPrice};{HighPrice};{LowPrice};{ClosePrice};{TotalVolume}";
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.StartFrom = DateTime.Today;
				_settings.Connection = null;
				_settings.BatchSize = 50;
				_settings.CheckUnique = true;
				_settings.TemplateTxtRegistry = new TemplateTxtRegistry();
			}
		}

		protected override TimeSpan OnProcess()
		{
			if (_settings.ExportType == ExportTypes.Sql && _settings.Connection == null)
			{
				this.AddErrorLog(LocalizedStrings.Str3768);
				return TimeSpan.MaxValue;
			}

			var allSecurity = this.GetAllSecurity();
			var supportedDataTypes = (allSecurity == null
					? Enumerable.Empty<Type>()
					: SupportedMarketDataTypes.Intersect(allSecurity.MarketDataTypes)
				).ToArray();

			this.AddInfoLog(LocalizedStrings.Str2306Params.Put(_settings.StartFrom));

			Func<int, bool> isCancelled = count => !CanProcess();

			var hasSecurities = false;

			foreach (var security in GetWorkingSecurities())
			{
				hasSecurities = true;

				if (!CanProcess())
					break;

				var path = _settings.ExportFolder;

				if (path.IsEmpty())
					path = DriveCache.Instance.DefaultDrive.Path;

				foreach (var t in (allSecurity == null ? security.MarketDataTypes : supportedDataTypes))
				{
					if (!CanProcess())
						break;

					var arg = _settings.CandleSettings.Arg;
					var dataType = t.ToMessageType(ref arg);

					this.AddInfoLog(LocalizedStrings.Str3769Params.Put(security.Security.Id, dataType.Name, _settings.ExportType));

					var fromStorage = StorageRegistry.GetStorage(security.Security, dataType, arg, _settings.Drive, _settings.StorageFormat);

					var from = fromStorage.GetFromDate();
					var to = fromStorage.GetToDate();

					if (from == null || to == null)
					{
						this.AddWarningLog(LocalizedStrings.Str3770);
						continue;
					}

					from = _settings.StartFrom.Max(from.Value);
					to = (DateTime.Today - TimeSpan.FromDays(_settings.Offset)).Min(to.Value);

					if (from > to)
						continue;

					BaseExporter exporter;

					if (_settings.ExportType == ExportTypes.Sql)
					{
						exporter = new DatabaseExporter(security.Security, arg, isCancelled, _settings.Connection)
						{
							BatchSize = _settings.BatchSize,
							CheckUnique = _settings.CheckUnique,
						};
					}
					else
					{
						var fileName = Path.Combine(path, security.Security.GetFileName(
							dataType, arg, from.Value, to.Value, _settings.ExportType));

						switch (_settings.ExportType)
						{
							case ExportTypes.Excel:
								exporter = new ExcelExporter(security.Security, arg, isCancelled, fileName, () => this.AddErrorLog(LocalizedStrings.Str3771));
								break;
							case ExportTypes.Xml:
								exporter = new XmlExporter(security.Security, arg, isCancelled, fileName);
								break;
							case ExportTypes.Txt:
								exporter = new TextExporter(security.Security, arg, isCancelled, fileName, GetTxtTemplate(dataType, arg));
								break;
							case ExportTypes.Bin:
								exporter = new BinExporter(security.Security, arg, isCancelled, DriveCache.Instance.GetDrive(path));
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}

					foreach (var date in from.Value.Range(to.Value, TimeSpan.FromDays(1)))
					{
						if (!CanProcess())
							break;

						try
						{
							this.AddInfoLog(LocalizedStrings.Str3772Params.Put(security.Security.Id, dataType.Name, _settings.ExportType, date));
							exporter.Export(dataType, fromStorage.Load(date));
						}
						catch (Exception ex)
						{
							HandleError(ex);
						}
					}
				}
			}

			if (!hasSecurities)
			{
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			if (CanProcess())
			{
				this.AddInfoLog(LocalizedStrings.Str2300);

				_settings.StartFrom = DateTime.Today - TimeSpan.FromDays(_settings.Offset);
				SaveSettings();
			}

			return base.OnProcess();
		}

		private string GetTxtTemplate(Type dataType, object arg)
		{
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			var registry = _settings.TemplateTxtRegistry;

			if (dataType == typeof(SecurityMessage))
				return registry.TemplateTxtSecurity;
			else if (dataType == typeof(NewsMessage))
				return registry.TemplateTxtNews;
			else if (dataType.IsSubclassOf(typeof(CandleMessage)))
				return registry.TemplateTxtCandle;
			else if (dataType == typeof(Level1ChangeMessage))
				return registry.TemplateTxtLevel1;
			else if (dataType == typeof(QuoteChangeMessage))
				return registry.TemplateTxtDepth;
			else if (dataType == typeof(ExecutionMessage))
			{
				if (arg == null)
					throw new ArgumentNullException("arg");

				switch ((ExecutionTypes)arg)
				{
					case ExecutionTypes.Tick:
						return registry.TemplateTxtTick;
					case ExecutionTypes.Order:
					case ExecutionTypes.Trade:
						return registry.TemplateTxtTransaction;
					case ExecutionTypes.OrderLog:
						return registry.TemplateTxtOrderLog;
					default:
						throw new InvalidOperationException(LocalizedStrings.Str1122Params.Put(arg));
				}
			}
			else
				throw new ArgumentOutOfRangeException("dataType", dataType, LocalizedStrings.Str721);
		}
	}
}