using System.Reflection;
using System.Windows;

namespace CsDocReplaceTool {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        void App_OnStartup(object sender, StartupEventArgs e) {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if(!string.IsNullOrEmpty(path)) System.IO.Directory.SetCurrentDirectory(path);
        }
    }
}
