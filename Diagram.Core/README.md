# Diagram.Core

Diagram.Core provides the fundamental building blocks for StockSharp's visual strategy designer. It contains the classes that allow trading algorithms to be represented as diagrams and executed as regular strategies.

## Overview

- **Diagram Elements and Sockets** — base types such as `DiagramElement` and `DiagramSocket` define nodes and their connection points. Elements can emit and receive values to build complex trading logic.
- **Composite Elements** — `CompositionDiagramElement` manages nested diagrams and exposes parameters and sockets of child elements.
- **Strategy Integration** — `DiagramStrategy` runs a diagram as a regular `Strategy`, enabling optimization and backtesting.
- **Undo/Redo Support** — interfaces like `IUndoManager` provide transaction based change tracking.
- **External Code** — the `DiagramExternalAttribute` allows methods to be exposed as diagram elements. A small helper script (`python/designer_extensions.py`) makes this available in Python.

## Getting Started

1. Reference **StockSharp.Diagram.Core** from your project (via the StockSharp NuGet feed or by adding the project to your solution).
2. Create diagram elements by deriving from `DiagramElement` and define sockets and parameters.
3. Combine elements inside a `CompositionDiagramElement` or execute them through `DiagramStrategy`.

Example of exposing a Python function as an external element:

```python
import clr
clr.AddReference("StockSharp.Diagram.Core")
from StockSharp.Diagram import DiagramExternalAttribute

# Decorator to mark methods as external diagram elements
def diagram_external(func):
    func.__dict__['__diagram_external__'] = DiagramExternalAttribute()
    return func
```

The above decorator adds the `DiagramExternalAttribute` to Python functions so they can be used in the Designer.


