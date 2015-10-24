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
            MainDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ConfigurationConstants.StockSharp,
                ConfigurationConstants.ApplicationName);
            CreateDirectoryIfNeeded(MainDirectory);

            ChartTemplateDirectory = Path.Combine(MainDirectory, ConfigurationConstants.ChartTemplates);
            CreateDirectoryIfNeeded(ChartTemplateDirectory);

            CodeDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Code);
            CreateDirectoryIfNeeded(CodeDirectory);

            LogsDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Logs);
            CreateDirectoryIfNeeded(LogsDirectory);

            SettingsDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Settings);
            CreateDirectoryIfNeeded(SettingsDirectory);

            ReportsDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Reports);
            CreateDirectoryIfNeeded(ReportsDirectory);

            ScreenshotDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Screenshots);
            CreateDirectoryIfNeeded(ScreenshotDirectory);

            SettingsDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Settings);
            CreateDirectoryIfNeeded(SettingsDirectory);

            SoundDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Sounds);
            CreateDirectoryIfNeeded(SoundDirectory);

            WatchListDirectory = Path.Combine(MainDirectory, ConfigurationConstants.WatchList);
            CreateDirectoryIfNeeded(WatchListDirectory);

            WorkspaceDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Workspace);
            CreateDirectoryIfNeeded(WorkspaceDirectory);



            LayoutDirectory = Path.Combine(WorkspaceDirectory, ConfigurationConstants.Layout);
            CreateDirectoryIfNeeded(LayoutDirectory);

            IndicatorsDirectory = Path.Combine(CodeDirectory, ConfigurationConstants.Indicators);
            CreateDirectoryIfNeeded(IndicatorsDirectory);

            StrategiesDirectory = Path.Combine(CodeDirectory, ConfigurationConstants.Strategies);
            CreateDirectoryIfNeeded(StrategiesDirectory);



            LayoutFileName = ConfigurationConstants.ApplicationName + ConfigurationConstants.Layout + ConfigurationConstants.XmlFileExtension;
            LogsFileName = ConfigurationConstants.ApplicationName + ConfigurationConstants.Logs + ConfigurationConstants.XmlFileExtension;
            SettingFileName = ConfigurationConstants.ApplicationName + ConfigurationConstants.Settings + ConfigurationConstants.XmlFileExtension;
        }

        public string MainDirectory { get; }
        public string WorkspaceDirectory { get; }
        public string ChartTemplateDirectory { get; }
        public string ReportsDirectory { get; }
        public string CodeDirectory { get; }
        public string IndicatorsDirectory { get; }
        public string LayoutDirectory { get; set; }
        public string LogsDirectory { get; set; }
        public string SettingsDirectory { get; set; }
        public string StrategiesDirectory { get; }
        public string SoundDirectory { get; }
        public string ScreenshotDirectory { get; }
        public string WatchListDirectory { get; }

        public string LayoutFileName { get; set; }
        public string LogsFileName { get; set; }
        public string SettingFileName { get; private set; }


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