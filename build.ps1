# build.ps1 - lightweight wrapper
# Usage: ./build.ps1 -Target Pack -SemVer 1.2.3 -Prerelease rc.1 -Push:$false
param(
  [string]$Target = "Default",
  [string]$Configuration = "Release",
  [string]$SemVer = "",
  [string]$Prerelease = "",
  [switch]$SkipTests,
  [switch]$SkipIntegration,
  [switch]$Push,
  [string]$NuGetSource = "",
  [string]$Solution = "./Maurer.OktaFilter/Maurer.OktaFilter.sln",
  [string]$Project = "./Maurer.OktaFilter/Maurer.OktaFilter/Maurer.OktaFilter.csproj"
)

dotnet tool restore
$cmd = "dotnet cake --target=$Target --configuration=$Configuration --solution=`"$Solution`" --project=`"$Project`""
if ($SemVer) { $cmd += " --semver=$SemVer" }
if ($Prerelease) { $cmd += " --prerelease=$Prerelease" }
if ($SkipTests) { $cmd += " --skipTests=true" }
if ($SkipIntegration) { $cmd += " --skipIntegration=true" }
if ($Push) { $cmd += " --push=true" }
if ($NuGetSource) { $cmd += " --nugetSource=`"$NuGetSource`"" }

Write-Host $cmd
iex $cmd
