namespace StockSharp.Studio
{
	using System.Windows;
	using System.Windows.Media.Animation;

	public partial class App
	{
		public App()
		{
			AppIcon = "/stocksharp_studio.ico";
			CheckTargetPlatform = true;

			// уменьшаем framerate для анимации
			Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 20 });
		}
	}
}