using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SciTrader.Model
{
    public class StockItem : INotifyPropertyChanged
    {
        private string _itemCode;
        private string _fullCode;
        private string _nameEn;
        private string _nameKr;
        private int _remainDays;
        private string _lastTradeDay;
        private string _highLimitPrice;
        private string _lowLimitPrice;
        private string _predayClose;
        private string _standardPrice;
        private string _strike;
        private int _atmType;
        private int _recentMonth;
        private string _expireDay;
        private int _id = 0;
        private double _seungSu = 250000;
        private int _decimal = 2;
        private double _contractSize = 0.05;
        private double _tickValue = 12500;
        private double _tickSize = 0.05;
        private string _marketName;
        private string _productCode;
        private int _totalVolume = 0;
        private int _predayVolume = 0;
        private string _deposit;
        private string _startTime;
        private string _endTime;
        private string _predayUpdownRate;
        private string _currency;
        private string _exchange;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ItemCode
        {
            get => _itemCode;
            set { _itemCode = value; OnPropertyChanged(nameof(ItemCode)); }
        }

        public string FullCode
        {
            get => _fullCode;
            set { _fullCode = value; OnPropertyChanged(nameof(FullCode)); }
        }

        public string NameEn
        {
            get => _nameEn;
            set { _nameEn = value; OnPropertyChanged(nameof(NameEn)); }
        }

        public string NameKr
        {
            get => _nameKr;
            set { _nameKr = value; OnPropertyChanged(nameof(NameKr)); }
        }

        public int RemainDays
        {
            get => _remainDays;
            set { _remainDays = value; OnPropertyChanged(nameof(RemainDays)); }
        }

        public string LastTradeDay
        {
            get => _lastTradeDay;
            set { _lastTradeDay = value; OnPropertyChanged(nameof(LastTradeDay)); }
        }

        public string HighLimitPrice
        {
            get => _highLimitPrice;
            set { _highLimitPrice = value; OnPropertyChanged(nameof(HighLimitPrice)); }
        }

        public string LowLimitPrice
        {
            get => _lowLimitPrice;
            set { _lowLimitPrice = value; OnPropertyChanged(nameof(LowLimitPrice)); }
        }

        public string PredayClose
        {
            get => _predayClose;
            set { _predayClose = value; OnPropertyChanged(nameof(PredayClose)); }
        }

        public string StandardPrice
        {
            get => _standardPrice;
            set { _standardPrice = value; OnPropertyChanged(nameof(StandardPrice)); }
        }

        public string Strike
        {
            get => _strike;
            set { _strike = value; OnPropertyChanged(nameof(Strike)); }
        }

        public int AtmType
        {
            get => _atmType;
            set { _atmType = value; OnPropertyChanged(nameof(AtmType)); }
        }

        public int RecentMonth
        {
            get => _recentMonth;
            set { _recentMonth = value; OnPropertyChanged(nameof(RecentMonth)); }
        }

        public string ExpireDay
        {
            get => _expireDay;
            set { _expireDay = value; OnPropertyChanged(nameof(ExpireDay)); }
        }

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public double SeungSu
        {
            get => _seungSu;
            set { _seungSu = value; OnPropertyChanged(nameof(SeungSu)); }
        }

        public int Decimal
        {
            get => _decimal;
            set { _decimal = value; OnPropertyChanged(nameof(Decimal)); }
        }

        public double ContractSize
        {
            get => _contractSize;
            set { _contractSize = value; OnPropertyChanged(nameof(ContractSize)); }
        }

        public double TickValue
        {
            get => _tickValue;
            set { _tickValue = value; OnPropertyChanged(nameof(TickValue)); }
        }

        public double TickSize
        {
            get => _tickSize;
            set { _tickSize = value; OnPropertyChanged(nameof(TickSize)); }
        }

        public string MarketName
        {
            get => _marketName;
            set { _marketName = value; OnPropertyChanged(nameof(MarketName)); }
        }

        public string ProductCode
        {
            get => _productCode;
            set { _productCode = value; OnPropertyChanged(nameof(ProductCode)); }
        }

        public int TotalVolume
        {
            get => _totalVolume;
            set { _totalVolume = value; OnPropertyChanged(nameof(TotalVolume)); }
        }

        public int PredayVolume
        {
            get => _predayVolume;
            set { _predayVolume = value; OnPropertyChanged(nameof(PredayVolume)); }
        }

        public string Deposit
        {
            get => _deposit;
            set { _deposit = value; OnPropertyChanged(nameof(Deposit)); }
        }

        public string StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(nameof(StartTime)); }
        }

        public string EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(nameof(EndTime)); }
        }

        public string PredayUpdownRate
        {
            get => _predayUpdownRate;
            set { _predayUpdownRate = value; OnPropertyChanged(nameof(PredayUpdownRate)); }
        }

        public string Currency
        {
            get => _currency;
            set { _currency = value; OnPropertyChanged(nameof(Currency)); }
        }

        public string Exchange
        {
            get => _exchange;
            set { _exchange = value; OnPropertyChanged(nameof(Exchange)); }
        }
    }
}
