using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;

using Pusher;
using WsPusher = Pusher.Pusher;

namespace Bitstamp
{
	public delegate void TradeHandler( Trade trade );
	public delegate void DepthHandler( Depth depth );
	public delegate void ErrorHandler( String text );

    public sealed class BitstampFlow
    {
		public event TradeHandler OnTrade = t => { }; // не надо проверять на null
		public event DepthHandler OnDepth = d => { };
		public event ErrorHandler OnError = e => { };

		public void Start()
		{
			if (_pusher != null)
				throw new InvalidOperationException( "Already started" );

			_pusher = new WsPusher( new Pusher.Connections.Net.WebSocketConnectionFactory(), 
				"de504dc5763aeef9ff52", 
				new Options() { Scheme = WebServiceScheme.Secure } );

			_pusher.EventEmitted += async ( s, a ) =>
			{
				if (a.EventName == "pusher:connection_established" && _trades == null)
				{
					try
					{
						_trades = await _pusher.SubscribeToChannelAsync( "live_trades" );
						_trades.EventEmitted += ( s2, a2 ) =>
						{
							if (a2.EventName == "trade")
								OnTrade( JsonConvert.DeserializeObject<Trade>( a2.Data ) );
						};

						_depth = await _pusher.SubscribeToChannelAsync( "order_book" );
						_depth.EventEmitted += ( s2, a2 ) =>
						{
							if (a2.EventName == "data")
								OnDepth( JsonConvert.DeserializeObject<Depth>( a2.Data ) );
						};
					}
					catch (Exception err)
					{
						OnError( err.ToString() );
					}
				}

				if (a.EventName == "pusher:error")
					OnError( a.Data );
			};

			_pusher.ConnectAsync().Wait();
		}
	
		public void Stop()
		{
			if (_pusher == null)
				throw new InvalidOperationException( "Not started" );

			_pusher.Disconnect();
			_pusher = null;
			_trades = _depth = null;
		}

		#region internals
		volatile WsPusher _pusher;
		Channel _trades, _depth;
		#endregion
	}
}
