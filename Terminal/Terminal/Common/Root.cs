namespace Terminal
{
    using ActiproSoftware.Windows;

    using StockSharp.Quik;
    using StockSharp.Algo;

    public class Root
    {
        private static Root _root;

        private Root()
        {
            Connector = new QuikTrader();
        }

        public static Root GetInstance()
        {
            if (_root == null)
            {
                lock (typeof(Root))
                {
                    _root = new Root();
                }
            }
            return _root;
        }

        public Connector Connector { private set; get; }

        private DeferrableObservableCollection<ToolItemViewModel> _toolItems;
        public DeferrableObservableCollection<ToolItemViewModel> ToolItems
        {
            get
            {
                if (_toolItems == null) ToolItems = new DeferrableObservableCollection<ToolItemViewModel>();
                return _toolItems;
            }
            set { _toolItems = value; }
        }

    }
}
