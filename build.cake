/*build.cake
* Cross-platform build for Maurer.OktaFilter
* Usage (from repo root):
* dotnet tool restore
* dotnet cake --target=Pack --semver=1.2.3
* Common targets: Clean, Restore, Build, Test, Pack, Push, Default*/

/*To Run Locally
dotnet new tool-manifest 
dotnet tool install Cake.Tool 
dotnet cake --target=Pack --skipIntegration=true*/

//#addin nuget:?package=Cake.Common&version=4.0.0
//#addin nuget:?package=Cake.Core&version=4.0.0
//#addin nuget:?package=Cake.DotNetTool.Module&version=1.0.1

var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var solution        = Argument("solution", "./Maurer.OktaFilter/Maurer.OktaFilter.sln");
var project         = Argument("project", "./Maurer.OktaFilter/Maurer.OktaFilter/Maurer.OktaFilter.csproj");
var semver          = Argument("semver", EnvironmentVariable("BUILD_SEMVER") ?? "0.1.0-local");
var prerelease      = Argument("prerelease", EnvironmentVariable("PRERELEASE") ?? "");
var skipTests       = Argument("skipTests", false);
var skipIntegration = Argument("skipIntegration", false);
var packArtifacts   = Argument("pack", true);
var pushPackages    = Argument("push", false);
var nugetSource     = Argument("nugetSource", EnvironmentVariable("NUGET_SOURCE") ?? "https://api.nuget.org/v3/index.json");
var apiKey          = EnvironmentVariable("NUGET_API_KEY");

var artifactsDir    = MakeAbsolute(Directory("./artifacts")).FullPath;

//////////////////////////////////////////////////////////////
// Helpers
//////////////////////////////////////////////////////////////

string ComputedVersion()
{
    // Prefer explicit semver argument/environment
    var v = semver;

    // Strip leading 'v' if provided
    if (!string.IsNullOrWhiteSpace(v) && v.StartsWith("v"))
    {
        v = v.Substring(1);
    }

    // Append prerelease if provided
    if (!string.IsNullOrWhiteSpace(prerelease))
    {
        v = $"{v}-{prerelease}";
    }
    return v;
}

//////////////////////////////////////////////////////////////
// Tasks
//////////////////////////////////////////////////////////////

Task("Info").Does(() =>
{
    Information("Configuration   : {0}", configuration);
    Information("Solution        : {0}", solution);
    Information("Project         : {0}", project);
    Information("Artifacts Dir   : {0}", artifactsDir);
    Information("Version         : {0}", ComputedVersion());
    Information("Skip Tests      : {0}", skipTests);
    Information("Skip Integration: {0}", skipIntegration);
    Information("Pack            : {0}", packArtifacts);
    Information("Push            : {0}", pushPackages);
    Information("NuGet Source    : {0}", nugetSource);
});

Task("Clean")
    .IsDependentOn("Info")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    // Clean common bin/obj folders
    var dirs = GetDirectories("./**/bin").Concat(GetDirectories("./**/obj"));
    foreach (var d in dirs) { try { CleanDirectory(d); } catch {} }
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore(solution);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var msbuild = new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true,
        MSBuildSettings = new DotNetMSBuildSettings()
            .WithProperty("ContinuousIntegrationBuild", "true")
            .WithProperty("Version", ComputedVersion())
    };

    DotNetBuild(solution, msbuild);
});

Task("Test")
    .IsDependentOn("Build")
    .WithCriteria(() => !skipTests)
    .Does(() =>
{
    var settings = new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
        ResultsDirectory = artifactsDir
    };

    settings.ArgumentCustomization = args =>
    {
        var a = args.Append("--logger").AppendQuoted("trx");        // write TRX results
        if (skipIntegration) a = a.Append("--filter").AppendQuoted("Type!=Integration");
        return a;
    };

    DotNetTest(solution, settings);
});

Task("Pack")
    .IsDependentOn("Test")
    .WithCriteria(() => packArtifacts)
    .Does(() =>
{
    var pack = new DotNetPackSettings
    {
        Configuration = configuration,
        NoBuild = true,
        OutputDirectory = artifactsDir,
        IncludeSymbols = true,
        SymbolPackageFormat = "snupkg",
        MSBuildSettings = new DotNetMSBuildSettings()
            .WithProperty("Version", ComputedVersion())
            .WithProperty("IncludeSource", "true")
            .WithProperty("IncludeSymbols", "true")
            .WithProperty("ContinuousIntegrationBuild", "true")
    };

    DotNetPack(project, pack);
});

Task("Push")
    .IsDependentOn("Pack")
    .WithCriteria(() => pushPackages && !string.IsNullOrWhiteSpace(apiKey))
    .Does(() =>
{
    var pkgs = GetFiles($"{artifactsDir}/*.nupkg");
    foreach (var nupkg in pkgs)
    {
        Information("Pushing {0} -> {1}", nupkg.GetFilename().FullPath, nugetSource);
        DotNetNuGetPush(nupkg.FullPath, new DotNetNuGetPushSettings
        {
            Source = nugetSource,
            ApiKey = apiKey,
            SkipDuplicate = true
        });
    }
});

Task("Default")
    .IsDependentOn("Pack");

RunTarget(target);
