namespace StockSharp.Charting;

/// <summary>
/// Enumeration constants to define label placement.
/// </summary>
public enum LabelPlacement
{
	/// <summary>Places on the right.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RightKey)]
	Right,
	
	/// <summary>Places at the top right.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TopRightKey)]
	TopRight,
	
	/// <summary>Places at the bottom right.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BottomRightKey)]
	BottomRight,
	
	/// <summary>Places at the bottom.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BottomKey)]
	Bottom,
	
	/// <summary>Places on the left.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LeftKey)]
	Left,
	
	/// <summary>Places on the top left.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TopLeftKey)]
	TopLeft,
	
	/// <summary>Places on the bottom left.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BottomLeftKey)]
	BottomLeft,
	
	/// <summary>Places at the top.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TopKey)]
	Top,
	
	/// <summary>Places on the axis.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AxisKey)]
	Axis,
	
	/// <summary>Places at the center.</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CenterKey)]
	Center,
	
	/// <summary>Automatic Placement (Default).</summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AutoKey)]
	Auto,
}
