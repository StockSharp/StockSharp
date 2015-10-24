using System;
using System.Collections.Generic;
using System.IO;
using Ecng.ComponentModel;
using StockSharp.Logging;

#pragma warning disable 1591

namespace StockSharp.Configuration.ConfigManager
{
    public class FolderManager
    {
        private readonly ConfigurationManager _configurationManager;
        
        private static readonly List<string> _directories = new List<string>(); 

        public FolderManager(ConfigurationManager configurationManager)
        {
            if (configurationManager == null) throw new ArgumentNullException(nameof(configurationManager));
            _configurationManager = configurationManager;

            // Create core directory structure.  Order is important for directory structure.
            MainDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                         ConfigurationConstants.StockSharp,
                                         ConfigurationConstants.ApplicationName);
            CheckCreateDirectory(MainDirectory);

            ChartTemplateDirectory = Path.Combine(MainDirectory, ConfigurationConstants.ChartTemplates);
            CheckCreateDirectory(ChartTemplateDirectory);

            CodeDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Code);
            CheckCreateDirectory(CodeDirectory);

            LogsDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Logs);
            CheckCreateDirectory(LogsDirectory);

            SettingsDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Settings);
            CheckCreateDirectory(SettingsDirectory);

            ReportsDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Reports);
            CheckCreateDirectory(ReportsDirectory);

            ScreenshotDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Screenshots);
            CheckCreateDirectory(ScreenshotDirectory);

            SettingsDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Settings);
            CheckCreateDirectory(SettingsDirectory);

            SoundDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Sounds);
            CheckCreateDirectory(SoundDirectory);

            WatchListDirectory = Path.Combine(MainDirectory, ConfigurationConstants.WatchList);
            CheckCreateDirectory(WatchListDirectory);

            WorkspaceDirectory = Path.Combine(MainDirectory, ConfigurationConstants.Workspace);
            CheckCreateDirectory(WorkspaceDirectory);



            ConnectionDirectory = Path.Combine(SettingsDirectory, ConfigurationConstants.Connection);
            CheckCreateDirectory(ConnectionDirectory);

            LayoutDirectory = Path.Combine(WorkspaceDirectory, ConfigurationConstants.Layout);
            CheckCreateDirectory(LayoutDirectory);

            IndicatorsDirectory = Path.Combine(CodeDirectory, ConfigurationConstants.Indicators);
            CheckCreateDirectory(IndicatorsDirectory);

            StrategiesDirectory = Path.Combine(CodeDirectory, ConfigurationConstants.Strategies);
            CheckCreateDirectory(StrategiesDirectory);

            

            ConnectionFileInfo = CreateNewFileInfo(ConnectionDirectory, ConfigurationConstants.ApplicationName + ConfigurationConstants.Connection + ConfigurationConstants.XmlFileExtension);
            LayoutFileInfo = CreateNewFileInfo(LayoutDirectory, ConfigurationConstants.ApplicationName + ConfigurationConstants.Layout + ConfigurationConstants.XmlFileExtension);
            LogsFileInfo = CreateNewFileInfo(LogsDirectory, ConfigurationConstants.ApplicationName + ConfigurationConstants.Logs + ConfigurationConstants.XmlFileExtension);
            SettingFileInfo = CreateNewFileInfo(SettingsDirectory, ConfigurationConstants.ApplicationName + ConfigurationConstants.Settings + ConfigurationConstants.XmlFileExtension);
        }

        public string ConnectionDirectory { get; }
        public string ChartTemplateDirectory { get; }
        public string CodeDirectory { get; }
        public string IndicatorsDirectory { get; }
        public string LayoutDirectory { get; set; }
        public string LogsDirectory { get; set; }
        public string MainDirectory { get; }
        public string ReportsDirectory { get; }
        public string SettingsDirectory { get; set; }
        public string StrategiesDirectory { get; }
        public string SoundDirectory { get; }
        public string ScreenshotDirectory { get; }
        public string WatchListDirectory { get; }
        public string WorkspaceDirectory { get; }


        public FileInfo ConnectionFileInfo { get; private set; }
        public FileInfo LayoutFileInfo { get; private set; }
        public FileInfo LogsFileInfo { get; private set; }
        public FileInfo SettingFileInfo { get; private set; }



        private FileInfo CreateNewFileInfo(string connectionDirectory, string connectionFileName)
        {
            return new FileInfo(Path.Combine(connectionDirectory, connectionFileName));
        }


        public static void CheckCreateDirectory([NotNull] string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentNullException(nameof(directory));

            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _directories.Add(directory);
                }
            }
            catch (Exception ex)
            {
                ex.LogError(string.Format("Unable to create directory " + directory, ex));
            }
        }
    }
}