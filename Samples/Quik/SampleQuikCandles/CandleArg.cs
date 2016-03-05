namespace SampleQuikCandles
{
	using Ecng.Common;

	class CandleArg : Wrapper<object>
	{
		public override Wrapper<object> Clone()
		{
			return new CandleArg { Value = base.Value };
		}
	}
}