namespace StockSharp.Diagram;

using System.Reflection;

/// <summary>
/// Attribute, applied to methods or parameters, to create input socket.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
public class DiagramExternalAttribute : Attribute
{
	/// <summary>
	/// Check if the member is marked with <see cref="DiagramExternalAttribute"/>.
	/// </summary>
	public static bool IsExternal(MemberInfo member)
		=> member.GetAttribute<DiagramExternalAttribute>() is not null;
}