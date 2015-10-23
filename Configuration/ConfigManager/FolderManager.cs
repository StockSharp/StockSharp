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
            MainDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ConfigConstants.PlatformName);
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

            SoundDirectory = Path.Combine(MainDirectory, ConfigConstants.Sounds);
            CreateDirectoryIfNeeded(SoundDirectory);

            WatchListDirectory = Path.Combine(MainDirectory, ConfigConstants.WatchList);
            CreateDirectoryIfNeeded(WatchListDirectory);

            WorkspaceDirectory = Path.Combine(MainDirectory, ConfigConstants.Workspace);
            CreateDirectoryIfNeeded(WorkspaceDirectory);

            IndicatorsDirectory = Path.Combine(CodeDirectory, ConfigConstants.Indicators);
            CreateDirectoryIfNeeded(IndicatorsDirectory);

            LayoutDirectory = Path.Combine(SettingsDirectory, ConfigConstants.Settings);
            CreateDirectoryIfNeeded(LayoutDirectory);

            StrategiesDirectory = Path.Combine(CodeDirectory, ConfigConstants.Strategies);
            CreateDirectoryIfNeeded(StrategiesDirectory);

            LayoutFile = ConfigConstants.PlatformName + ConfigConstants.Layout + ".xml";
            SettingFile = Path.Combine(MainDirectory, ConfigConstants.PlatformName + ConfigConstants.Settings);
        }

        public string MainDirectory { get; private set; }
        public string WorkspaceDirectory { get; private set; }
        public string ChartTemplateDirectory { get; private set; }
        public string ReportsDirectory { get; private set; }
        public string CodeDirectory { get; private set; }
        public string IndicatorsDirectory { get; private set; }
        public string LayoutDirectory { get; set; }
        public string LayoutFile { get; set; }
        public string SettingFile { get; private set; }
        public string SettingsDirectory { get; set; }
        public string StrategiesDirectory { get; private set; }
        public string SoundDirectory { get; private set; }
        public string ScreenshotDirectory { get; private set; }
        public string WatchListDirectory { get; private set; }


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