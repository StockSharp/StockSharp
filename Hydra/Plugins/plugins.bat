set tmp=%1
set proj=%tmp:Public=%

md ..\..\..\..\Hydra\bin\%2\Plugins

copy StockSharp.Hydra.%proj%.dll ..\..\..\..\Hydra\bin\%2\Plugins\StockSharp.Hydra.%proj%.dll

if %2 == Debug goto :debug

goto :exit

:debug
copy StockSharp.Hydra.%proj%.pdb ..\..\..\..\Hydra\bin\%2\Plugins\StockSharp.Hydra.%proj%.pdb

:exit