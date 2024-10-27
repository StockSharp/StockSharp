using DevExpress.Xpf.Core;
using System;
using System.ComponentModel;
using System.Windows.Media;

namespace SciTrader.Data {
    public class TradingData : INotifyPropertyChanged {
        double open;
        double high;
        double low;
        double close;
        double volume;
        Color volumeColor;

        public bool UpdateSuspended { get; private set; }
        public DateTime Date { get; set; }
        public double Open {
            get { return open; }
            set { 
                if(open != value) {
                    open = value;
                    RaisePropertyChanged("Open");
                    UpdateVolumeColor();
                }
            }
        }
        public double High {
            get { return high; }
            set {
                if (high != value) {
                    high = value;
                    RaisePropertyChanged("High");
                }
            }
        }
        public double Low {
            get { return low; }
            set { 
                if(low != value) {
                    low = value;
                    RaisePropertyChanged("Low");
                }
            }
        }
        public double Close {
            get { return close; }
            set { 
                if(close != value) {
                    close = value;
                    RaisePropertyChanged("Close");
                    UpdateVolumeColor();
                }
            }
        }
        public double Volume {
            get { return volume; }
            set {
                if(volume != value) {
                    volume = value;
                    RaisePropertyChanged("Volume");
                }
            }
        }
        public Color VolumeColor {
            get {
                return volumeColor;
            }
            set {
                if(volumeColor != value) {
                    volumeColor = value;
                    RaisePropertyChanged("VolumeColor");
                }
            }
        }

        public TradingData(DateTime date, double open, double high, double low, double close, double volume) {
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            UpdateVolumeColor();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void RaisePropertyChanged(string propertyName) {
            if (PropertyChanged != null && !UpdateSuspended)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        void UpdateVolumeColor()
        {
            try
            {
                VolumeColor = close >= open ?
                    (Color)App.Current.Resources["lightGreenColor"] :
                    (Color)App.Current.Resources["lightRedColor"];
            }
            catch (Exception ex)
            {
                // Handle the exception, log it, or display an error message
                Console.WriteLine("An error occurred while updating the volume color: " + ex.Message);
            }
        }


        public void SuspendUpdate() {
            UpdateSuspended = true;
        }
        public void ResumeUpdate() {
            UpdateSuspended = false;
        }
    }
}
