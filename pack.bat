"c:\program files\dotnet\dotnet.exe" pack -o pub -c Release --no-build StockSharp.sln
rd /S /Q pub\out
pub\nuget.exe init pub pub\out -Expand
del "pub\*.nupkg"
