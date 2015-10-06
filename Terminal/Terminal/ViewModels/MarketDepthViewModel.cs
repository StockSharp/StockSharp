namespace Terminal
{
    using StockSharp.BusinessEntities;
    public class MarketDepthViewModel : ToolItemViewModel
    {

        private Security _security;
        public Security Security
        {
            get { return _security; }
            set
            {
                if (_security == value) return;
                _security = value;
                NotifyPropertyChanged();
            }
        }

    }
}
