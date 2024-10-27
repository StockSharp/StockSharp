using SciTrader.ViewModels;
using SciTrader.Views;
using DevExpress.Xpf.Core;

namespace SciTrader {
    public partial class MainWindow : ThemedWindow {
        public MainWindow() {
            InitializeComponent();
        }

        void OnInformationPanelLoaded(object sender, System.Windows.RoutedEventArgs e) {
            ((InformationPanel)sender).DataContext = ((MainViewModel)DataContext).InformationPanelModel;
        }
    }
}
