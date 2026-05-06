dotnet build -c Release
#dotnet pack -c Release 

dotnet pack -c Release -p:Version=$1 --output ./nupkg
