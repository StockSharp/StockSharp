namespace Terminal
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    using StockSharp.BusinessEntities;

    using MoreLinq;

    public partial class MarketDepthView : UserControl
    {
        private readonly Root _root;
        public MarketDepthView()
        {
            InitializeComponent();
            _root = Root.GetInstance();
            _root.Connector.MarketDepthsChanged += OnMarketDepthChanged;

        }

        public Security Security
        {
            get { return (Security)this.GetValue(SecurityProperty); }
            set { this.SetValue(SecurityProperty, value); }
        }

        public static readonly DependencyProperty SecurityProperty = DependencyProperty.Register(
          "Security", typeof(Security), typeof(MarketDepthView), new PropertyMetadata(null, new PropertyChangedCallback(OnSecurityPropertyChanged)));

        private static void OnSecurityPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var mdView = (MarketDepthView)obj;
            var security = (Security)e.NewValue;

            if (security == null) return;

            mdView.Depth.UpdateFormat(security);

            if (!mdView._root.Connector.RegisteredMarketDepths.Contains(security))
                mdView._root.Connector.RegisterMarketDepth(security);
        }

        private void OnMarketDepthChanged(IEnumerable<MarketDepth> depths)
        {
            depths.ForEach(d => { Depth.UpdateDepth(d); });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _root.Connector.MarketDepthsChanged -= OnMarketDepthChanged;
        }
    }
}
