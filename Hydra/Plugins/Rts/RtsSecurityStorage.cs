namespace Ecng.Trading.Hydra.Rts
{
	using Ecng.Trading.Hydra.Core;

	class RtsSecurityStorage : SecurityStorage
	{
		public RtsSecurityStorage(RtsTradeLoader loader)
			: base(loader)
		{
		}
	}
}