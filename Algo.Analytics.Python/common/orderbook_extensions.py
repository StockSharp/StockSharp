import clr

# Add references to required assemblies
clr.AddReference("StockSharp.Messages")

from StockSharp.Messages import Extensions

def get_best_bid(message):
    """Get best bid using C# extension method."""
    return Extensions.GetBestBid(message)

def get_best_ask(message):
    """Get best ask using C# extension method."""
    return Extensions.GetBestAsk(message)