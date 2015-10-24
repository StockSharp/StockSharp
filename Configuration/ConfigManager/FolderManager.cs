using System;
using System.IO;
using Ecng.ComponentModel;
using StockSharp.Logging;

#pragma warning disable 1591

namespace StockSharp.Configuration.ConfigManager
{
    public class FolderManager
    {
        private readonly ConfigurationManager _configurationManager;

        public FolderManager(ConfigurationManager configurationManager)
        {
            if (configurationManager == null) throw new ArgumentNullException(nameof(configurationManager));
            _configurationManager = configurationManager;

            // NOTE: order important for directory structure
            MainDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ConfigConstants.ApplicationName);
            CreateDirectoryIfNeeded(MainDirectory);

            ChartTemplateDirectory = Path.Combine(MainDirectory, ConfigConstants.ChartTemplates);
            CreateDirectoryIfNeeded(ChartTemplateDirectory);

            CodeDirectory = Path.Combine(MainDirectory, ConfigConstants.Code);
            CreateDirectoryIfNeeded(CodeDirectory);

            SettingsDirectory = Path.Combine(MainDirectory, ConfigConstants.Settings);
            CreateDirectoryIfNeeded(SettingsDirectory);

            ReportsDirectory = Path.Combine(MainDirectory, ConfigConstants.Reports);
            CreateDirectoryIfNeeded(ReportsDirectory);

            ScreenshotDirectory = Path.Combine(MainDirectory, ConfigConstants.Screenshots);
            CreateDirectoryIfNeeded(ScreenshotDirectory);

            SettingsDirectory = Path.Combine(MainDirectory, ConfigConstants.Settings);
            CreateDirectoryIfNeeded(SettingsDirectory);

            SoundDirectory = Path.Combine(MainDirectory, ConfigConstants.Sounds);
            CreateDirectoryIfNeeded(SoundDirectory);

            WatchListDirectory = Path.Combine(MainDirectory, ConfigConstants.WatchList);
            CreateDirectoryIfNeeded(WatchListDirectory);

            WorkspaceDirectory = Path.Combine(MainDirectory, ConfigConstants.Workspace);
            CreateDirectoryIfNeeded(WorkspaceDirectory);

            LayoutDirectory = Path.Combine(WorkspaceDirectory, ConfigConstants.Layout);
            CreateDirectoryIfNeeded(LayoutDirectory);

            IndicatorsDirectory = Path.Combine(CodeDirectory, ConfigConstants.Indicators);
            CreateDirectoryIfNeeded(IndicatorsDirectory);

            StrategiesDirectory = Path.Combine(CodeDirectory, ConfigConstants.Strategies);
            CreateDirectoryIfNeeded(StrategiesDirectory);

            LayoutFileName = ConfigConstants.ApplicationName + ConfigConstants.Layout + ".xml";
            SettingFile = Path.Combine(MainDirectory, ConfigConstants.ApplicationName + ConfigConstants.Settings);
        }

        public string MainDirectory { get; }
        public string WorkspaceDirectory { get; }
        public string ChartTemplateDirectory { get; }
        public string ReportsDirectory { get; }
        public string CodeDirectory { get; }
        public string IndicatorsDirectory { get; }
        public string LayoutDirectory { get; set; }
        public string LayoutFileName { get; set; }
        public string SettingFile { get; private set; }
        public string SettingsDirectory { get; set; }
        public string StrategiesDirectory { get; }
        public string SoundDirectory { get; }
        public string ScreenshotDirectory { get; }
        public string WatchListDirectory { get; }


        public static void CreateDirectoryIfNeeded([NotNull] string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentNullException(nameof(directory));

            try
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                ex.LogError(string.Format("Unable to create directory " + directory, ex));
            }
        }
    }
}