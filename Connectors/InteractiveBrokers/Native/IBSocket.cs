namespace StockSharp.InteractiveBrokers.Native
{
	using System;
	using System.Globalization;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;

	using Ecng.Common;
	using Ecng.Net;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Localization;

	class IBSocket : BaseLogReceiver
	{
		private static readonly byte[] _eol = { 0 };
		private NetworkStream _outgoing;
		private NetworkStream _incoming;
		private TcpClient _client;

		// после обфускации название типа нечитаемо
		public override string Name
		{
			get { return "IBSocket"; }
		}

		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			if (_client == null)
				return;

			if (_incoming != null)
			{
				_incoming.Dispose();
				_outgoing.Dispose();
			}

			_client.Close();
			_client = null;
		}

		/// <summary>
		/// Returns the version of the TWS instance the API application is connected to
		/// </summary>
		public ServerVersions ServerVersion { get; internal set; }

		public bool IsConnected
		{
			get { return _client != null && _client.Client.IsConnected(); }
		}

		public void Connect(EndPoint address)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			if (_client != null)
				throw new InvalidOperationException(LocalizedStrings.Str2152);

			//this.AddInfoLog("BeginConnect");

			_client = new TcpClient();
			_client.Connect(address.GetHost(), address.GetPort());

			_incoming = _client.GetStream();
			_outgoing = _client.GetStream();
		}

		internal event Func<IBSocket, bool> ProcessResponse;

		public void StartListening(Action<Exception> processError)
		{
			if (processError == null)
				throw new ArgumentNullException("processError");

			ThreadingHelper
				.Thread(() =>
				{
					try
					{
						while (!IsDisposed)
						{
							var func = ProcessResponse;

							if (func == null || !func(this))
								break;
						}
					}
					catch (Exception ex)
					{
						if (!IsDisposed)
							processError(ex);
					}
				})
				.Name("IBSocket response")
				.Culture(CultureInfo.InvariantCulture)
				.Start();
		}

		public IBSocket Send(string str)
		{
			var stream = _outgoing;

			if (stream == null)
				throw new InvalidOperationException(LocalizedStrings.Str2153);

			this.AddDebugLog("Send: {0}", str);

			if (!str.IsEmpty())
				stream.WriteRaw(Encoding.UTF8.GetBytes(str));

			stream.WriteRaw(_eol);
			return this;
		}

		public IBSocket Send(int val)
		{
			return Send(val.To<string>());
		}

		public IBSocket Send(long val)
		{
			return Send(val.To<string>());
		}

		public IBSocket Send(decimal val)
		{
			return Send(val.To<string>());
		}

		public IBSocket Send(int? val)
		{
			return Send(val.To<string>());
		}

		public IBSocket Send(decimal? val)
		{
			return Send(val.To<string>());
		}

		public IBSocket Send(bool val)
		{
			return Send(val ? 1 : 0);
		}

		public IBSocket Send(bool? val)
		{
			return val != null ? Send(val.Value) : Send(string.Empty);
		}

		public IBSocket Send(DateTimeOffset? time, string format)
		{
			return Send(time == null ? string.Empty : time.Value.ToString(format));
		}

		public string ReadStr(bool blocked = true)
		{
			var stream = _incoming;

			if (stream == null)
				throw new InvalidOperationException(LocalizedStrings.Str2153);

			var buf = new StringBuilder();
			
			while (true)
			{
				var buffer = new byte[1];
				Exception error = null;

				var sync = new object();
				var isRead = false;

				stream.BeginRead(buffer, 0, 1, ar =>
				{
					try
					{
						stream.EndRead(ar);
					}
					catch (Exception ex)
					{
						error = ex;
					}

					lock (sync)
					{
						isRead = true;
						Monitor.Pulse(sync);
					}
				}, null);

				lock (sync)
				{
					if (!isRead)
						Monitor.Wait(sync);
				}

				if (error != null)
				{
					if (blocked)
						throw error;
					else
						return null;
				}

				if (buffer[0] == 0)
					break;

				buf.Append((char)buffer[0]);

				//var b = (byte)stream.ReadByte();

				//if (b == 0)
				//	break;

				//buf.Append((char)b);
			}

			var str = buf.ToString();
			this.AddDebugLog("Read: {0}", str);
			return str;
		}

		public bool ReadBool()
		{
			var value = ReadInt();

			switch (value)
			{
				case 0:
				case -1:
					return false;
				case 1:
					return true;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2528Params.Put(value));
			}
		}

		public int ReadInt()
		{
			return ReadNullInt() ?? 0;
		}

		public int? ReadNullInt()
		{
			var str = ReadStr();
			return str.IsEmpty() ? (int?)null : str.To<int>();
		}

		public long ReadLong()
		{
			var str = ReadStr();
			return str.IsEmpty() ? 0 : str.To<long>();
		}

		public decimal ReadDecimal()
		{
			return ReadNullDecimal() ?? 0;
		}

		public decimal? ReadNullDecimal()
		{
			var str = ReadStr();

			if (str.IsEmpty())
				return null;

			var value = str.To<double>();

			if (Math.Abs(value - double.MaxValue) < double.Epsilon)
				return null;

			return (decimal)value;
		}

		public DateTimeOffset ReadDateTime(string format)
		{
			return ReadStr().ToDateTime(format).ApplyTimeZone(TimeZoneInfo.Utc);
		}

		public DateTimeOffset? ReadNullDateTime(string format)
		{
			var str = ReadStr();
			return str.IsEmpty() ? (DateTimeOffset?)null : str.ToDateTime(str).ApplyTimeZone(TimeZoneInfo.Utc);
		}

		public DateTimeOffset ReadLongDateTime()
		{
			return TimeHelper.GregorianStart.AddSeconds(ReadLong());
			//Check if date time string or seconds
			//if (longDate < 30000000)
			//	time =
			//		new DateTime(Int32.Parse(date.Substring(0, 4)), Int32.Parse(date.Substring(4, 2)),
			//					 Int32.Parse(date.Substring(6, 2)), 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
			//else
		}
	}
}