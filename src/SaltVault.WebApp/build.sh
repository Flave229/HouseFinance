dotnet restore && dotnet build ./src/SaltVault.WebApp/SaltVault.WebApp.csproj 
&& dotnet test ./src/SaltVault.Tests/SaltVault.Tests.csproj -c Release -f netcoreapp2.0
&& dotnet test ./src/SaltVault.IntegrationTests/SaltVault.IntegrationTests.csproj -c Release --settings test.runsettings -f netcoreapp2.0 -- MSTest.MaxCpuCount=1