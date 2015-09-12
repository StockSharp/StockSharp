#pragma warning disable 169
namespace SampleRealTimeEmulation
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;

    using Ecng.Xaml;

    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Testing;
    using StockSharp.BusinessEntities;
    using StockSharp.Logging;
    using StockSharp.Messages;

    /// <summary>
    /// Helper class to demonstrate real time interaction.
    /// Implements chosing and changing charts and chart intervals real time.
    /// Upon building a new chart, the selected security is subscribed to trades and begins building candles.
    /// </summary>
    public class RealTimeHelper : INotifyPropertyChanged
    {
        private readonly MainWindow _parent;

        private bool _chartTypesPopulated;

        public RealTimeHelper(MainWindow mainWindow)
        {
            _parent = mainWindow;
            
            this.PopulateChartTypes();
        }

        public void PopulateChartTypes()
        {
            if (this._chartTypesPopulated) { return; }
            this._chartTypesPopulated = true;
            this.ChartTypeCollection = new ObservableCollection<Type>((from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                                                       from assemblyType in domainAssembly.GetTypes()
                                                                       where typeof(Candle).IsAssignableFrom(assemblyType)
                                                                       where assemblyType.Name != "Candle"
                                                                       select assemblyType).ToList());
        }

        public ObservableCollection<Type> ChartTypeCollection { get; private set; }

        private Type _candleType;

        public Type CandleType
        {
            get { return _candleType; }
            set
            {
                if (this._candleType == value) { return; }
                this._candleType = value;
                this.NotifyPropertyChanged("CandleType");

                // TODO: implement additional functionality

                if(this.ChartArg != null) this.CreateCandleChart(this._parent.MySecurity);
            }
        }

        private object _resolvedChartArg;

        private string _chartArg;

        public string ChartArg
        {
            get { return _chartArg; }
            set
            {
                if (this._chartArg == value) { return; }
                this._chartArg = value;
                this.NotifyPropertyChanged("ChartArg");

                // TODO: implement additional functionality

                if(this.CandleType != null && this._parent.MySecurity != null)
                    this.CreateCandleChart(this._parent.MySecurity);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) { handler(this, new PropertyChangedEventArgs(propertyName)); }
        }

        public void CreateCandleChart(Security security)
        {
            try
            {
                if (_parent.MyCandleSeries != null) _parent.MyCandleManager.Stop(_parent.MyCandleSeries);

                if (ChartArg == null || CandleType == null)
                {
                    string msg = string.Format("Chart interval = '{0}'.  Candle type = '{1}'.  Both must be valid to create the chart.", ChartArg, CandleType.Name);

                    Log(_parent, msg, LogLevels.Error);
                    return;
                }

                object resolvedChartArg = this.DeterminChartArg();

                _parent.MyCandleSeries = new CandleSeries(CandleType, security, resolvedChartArg);
                _parent.MyCandleManager.Start(_parent.MyCandleSeries);

                _parent.Log.Messages.Clear();
                Log(_parent, string.Format("Creating {0} real-time {1} chart. Interval = {2}", _parent.MySecurity, CandleType.Name, ChartArg), LogLevels.Debug);
            }
            catch (Exception e) 
            {
                Log(_parent, string.Format("Error trying to create {0} chart. \nException: {1} \nInnerException: {2}", CandleType.Name, e.Message, e.InnerException), LogLevels.Error);
            }
        }

        private object DeterminChartArg()
        {
            object resolvedChartArg = null;
            Type arg = null;
            object instance = null;

            instance = Activator.CreateInstance(this.CandleType);

            if (instance.GetType() == typeof(RenkoCandle)) { arg = typeof(Unit); }
            else if (instance.GetType() == typeof(RangeCandle)) { arg = typeof(Unit); }
            else if (instance.GetType() == typeof(PnFCandle)) { arg = typeof(PnFArg); }
            else
            { arg = Activator.CreateInstance(instance.GetPropValue("Arg").GetType()).GetType(); }

            if (arg == typeof(TimeSpan)) { resolvedChartArg = new TimeSpan(0, (int)Convert.ToDecimal(this.ChartArg), 0); }
            else if (arg == typeof(decimal)) { resolvedChartArg = Convert.ToDecimal(this.ChartArg); }
            else if (arg == typeof(Unit)) { resolvedChartArg = new Unit(Convert.ToDecimal(this.ChartArg)); }
            else if (arg == typeof(int)) { resolvedChartArg = (int)Convert.ToDecimal(this.ChartArg); }
            else if (arg == typeof(long)) { resolvedChartArg = (long)Convert.ToDecimal(this.ChartArg); }
            else if (arg == typeof(PnFArg)) { resolvedChartArg = new PnFArg { BoxSize = new Unit(Convert.ToDecimal(this.ChartArg)), ReversalAmount = (int)Convert.ToDecimal(this.ChartArg) }; }
            else
            { resolvedChartArg = (int)Convert.ToDecimal(this.ChartArg); }
            return resolvedChartArg;
        }

        private static void Log(MainWindow wnd, string message, LogLevels level)
        {
            Type asm = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                       from connector in assembly.GetTypes()
                       where typeof(BaseEmulationConnector).IsAssignableFrom(connector)
                       where connector.Name.Contains("Trader")
                       select connector).FirstOrDefault();


            ILogSource source = (from s in wnd.MyLogManager.Sources where s.Name.Contains(asm.Name) select s).FirstOrDefault();
            wnd.Log.Messages.Add(new LogMessage(source, new DateTimeOffset(DateTime.Now), level, message));
        }
    }
}
