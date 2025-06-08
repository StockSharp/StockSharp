namespace StockSharp.Diagram;

/// <summary>
/// Attribute, applied to methods or parameters, to create input socket.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
public class DiagramExternalAttribute : Attribute
{
}