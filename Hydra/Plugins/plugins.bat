md ..\..\..\..\Hydra\bin\%2\Plugins

copy StockSharp.Hydra.%1.dll ..\..\..\..\Hydra\bin\%2\Plugins\StockSharp.Hydra.%1.dll

if %2 == Debug goto :debug

goto :exit

:debug
copy StockSharp.Hydra.%1.pdb ..\..\..\..\Hydra\bin\%2\Plugins\StockSharp.Hydra.%1.pdb

:exit