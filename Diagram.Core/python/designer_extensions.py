import clr

clr.AddReference("StockSharp.Diagram.Core")
from StockSharp.Diagram import DiagramExternalAttribute

# Decorator to mark methods as external diagram elements
def diagram_external(func):
    # Apply the DiagramExternalAttribute to the function
    func.__dict__['__diagram_external__'] = DiagramExternalAttribute()
    return func