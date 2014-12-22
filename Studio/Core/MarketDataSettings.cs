namespace StockSharp.Studio.Core
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Community;

	public class MarketDataSettings : Equatable<MarketDataSettings>, IPersistable, INotifyPropertyChanged
	{
		private const string _serverUrl = "stocksharp.com";

		private static MarketDataSettings _stockSharpSettings;

		public static MarketDataSettings StockSharpSettings
		{
			get
			{
				return _stockSharpSettings ?? (_stockSharpSettings = new MarketDataSettings
				{
					UseLocal = false,
					Path = "net.tcp://{0}:8000".Put(_serverUrl),
					//Path = Directory.GetCurrentDirectory(),
					//IsAlphabetic = true,
				});
			}
		}

		private bool _useLocal;
		private string _path;
		//private bool _isAlphabetic;
		private Guid _id;

		public MarketDataSettings()
		{
			Credentials = new ServerCredentials();
			Id = Guid.NewGuid();
		}

		public Guid Id
		{
			get { return _id; }
			set
			{
				_id = value;
				OnPropertyChanged("Id");
			}
		}

		public bool UseLocal
		{
			get { return _useLocal; }
			set
			{
				_useLocal = value;
				OnPropertyChanged("UseLocal");
			}
		}

		public string Path
		{
			get { return _path; }
			set
			{
				_path = value;
				OnPropertyChanged("Path");
			}
		}

		//public bool IsAlphabetic
		//{
		//	get { return _isAlphabetic; }
		//	set
		//	{
		//		_isAlphabetic = value;
		//		OnPropertyChanged("IsAlphabetic");
		//	}
		//}

		public ServerCredentials Credentials { get; private set; }

		public bool IsStockSharpStorage
		{
			get { return !UseLocal && Path.ContainsIgnoreCase(_serverUrl); }
		}

		public override MarketDataSettings Clone()
		{
			return new MarketDataSettings
			{
				Id = Id,
				UseLocal = UseLocal,
				Path = Path,
				//IsAlphabetic = IsAlphabetic,
				Credentials = Credentials.Clone()
			};
		}

		protected override bool OnEquals(MarketDataSettings other)
		{
			return other.UseLocal == UseLocal && other.Path == Path/* && other.IsAlphabetic == IsAlphabetic*/;
		}

		#region IPersistable

		void IPersistable.Load(SettingsStorage storage)
		{
			Id = storage.GetValue<string>("Id").To<Guid>();
			Credentials = storage.GetValue<SettingsStorage>("Credentials").Load<ServerCredentials>();
			UseLocal = storage.GetValue<bool>("UseLocal");
			Path = storage.GetValue<string>("Path");
			//IsAlphabetic = storage.GetValue<bool>("IsAlphabetic");
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("Id", Id.To<string>());
			storage.SetValue("Credentials", Credentials.Save());
			storage.SetValue("UseLocal", UseLocal);
			storage.SetValue("Path", Path);
			//storage.SetValue("IsAlphabetic", IsAlphabetic);
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName = null)
		{
			var handler = PropertyChanged;

			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}