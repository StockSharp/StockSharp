namespace StockSharp.Studio
{
	using System;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	public partial class StrategyInfoCodeContent : IStudioControl, IStudioCommandScope
	{
		private readonly ResettableTimer _timer;

		private StrategyInfo _strategyInfo;

		public StrategyInfo StrategyInfo
		{
			get { return _strategyInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_strategyInfo = value;
				CodePanel.Code = _strategyInfo.Body;
			}
		}

		public StrategyInfoCodeContent()
		{
			InitializeComponent();

			_timer = new ResettableTimer(TimeSpan.FromSeconds(1));
			_timer.Elapsed += () => GuiDispatcher.GlobalDispatcher.AddAction(CompileCode);

			var persSvc = ConfigManager.GetService<IPersistableService>();
			CodePanel.References.AddRange(persSvc.GetReferences());
			CodePanel.ReferencesUpdated += () => persSvc.SetReferences(CodePanel.References);
			
			CodePanel.SavingCode += SaveCode;
			CodePanel.CompilingCode += CompileCode;
			CodePanel.CodeChanged += _timer.Reset;

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<CompileStrategyInfoResultCommand>(this, true, cmd => CodePanel.ShowCompilationResult(cmd.Result, GetStrategiesState()));
			cmdSvc.Register<EditReferencesCommand>(this, true, cmd => CodePanel.EditReferences());
		}

		private bool GetStrategiesState()
		{
			return _strategyInfo != null && _strategyInfo.Strategies.SyncGet(c => c.Any(s => s.ProcessState != ProcessStates.Stopped));
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			CodePanel.Load(storage.GetValue<SettingsStorage>("CodePanel"));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("CodePanel", CodePanel.Save());
		}

		void IDisposable.Dispose()
		{
			_timer.Flush();
		}

	    public string Title
		{
            get { return LocalizedStrings.Code; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}

		private void SaveCode()
		{
			_strategyInfo.Body = CodePanel.Code;

			ConfigManager.GetService<IStudioEntityRegistry>().Strategies.Save(_strategyInfo);
		}

		private void CompileCode()
		{
			SaveCode();
			new CompileStrategyInfoCommand(_strategyInfo, CodePanel.References.ToArray()).Process(this);
		}
	}
}