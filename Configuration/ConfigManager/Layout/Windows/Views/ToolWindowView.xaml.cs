using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using StockSharp.Configuration.ConfigManager.Layout.Windows.ViewModels;

namespace StockSharp.Configuration.ConfigManager.Layout.Windows.Views
{
    /// <summary>
    /// Interaction logic for ToolWindowView.xaml
    /// </summary>
    public partial class ToolWindowView : UserControl
    {
        /// <summary>
        /// Generic reusable view for <see cref="ToolWindowViewModel"/>
        /// </summary>
        public ToolWindowView()
        {
            InitializeComponent();
            this.DataContext = new ToolWindowViewModel();
        }
    }
}
