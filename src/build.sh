
dotnet build  --property WarningLevel=0     -v:detailed 2>&1 | grep -E "(CodeMechanic|generator|analyzer)"
#
#dotnet build -v:detailed | grep -i "CodeMechanic"
