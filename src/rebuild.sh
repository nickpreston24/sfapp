dotnet clean
rm -rf bin obj ~/.nuget/packages/codemechanic.embeds.sourcegen/ 
rm -rf obj/Generated   # if it exists

dotnet restore --force --no-cache
dotnet build --no-incremental
