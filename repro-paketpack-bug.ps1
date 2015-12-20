
devenv.com .\Composable.Everything.sln /build

.\buildpaket.ps1

echo "Look at the packages in the NuGetFeed folder. 
	Composable.CQRS.3.3.1-paket01.nupkg will have a dependency Composable.Core 3.3.1.0 even though the version should be 3.3.1-paket01
"
