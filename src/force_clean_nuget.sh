# 1. Nuke all caches
dotnet nuget locals all --clear

# 2. Clean project
dotnet clean
rm -rf bin obj ~/.nuget/packages/codemechanic.embeds.sourcegen/

# 3. Restore with force + detailed output
dotnet restore --force --no-cache -v:normal
