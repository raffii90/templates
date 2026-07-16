#tool "nuget:?package=NuGet.CommandLine&version=5.1.0"
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"

var target = Argument("Target", "Full");

Setup<BuildState>(_ => 
{
    var state = new BuildState
    {
        Paths = new BuildPaths
        {
            SolutionFile = MakeAbsolute(File("./Templates.slnx")),
            NuSpecFile = MakeAbsolute(File("./EMG.Templates.nuspec"))
        }
    };

    CleanDirectory(state.Paths.OutputFolder);

    return state;
});

Task("BuildTemplates")
    .Does<BuildState>(state => 
{
    DotNetCoreBuild(state.Paths.SolutionFile.ToString());
});

Task("Version")
    .Does<BuildState>(state =>
{
    var version = GitVersion();

    var packageVersion = version.SemVer;
    var buildVersion = $"{version.FullSemVer}+{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

    state.Version = new VersionInfo
    {
        PackageVersion = packageVersion,
        BuildVersion = buildVersion
    };

    Information($"Package version: {state.Version.PackageVersion}");
    Information($"Build version: {state.Version.BuildVersion}");

    if (BuildSystem.IsRunningOnAppVeyor)
    {
        AppVeyor.UpdateBuildVersion(state.Version.BuildVersion);
    }
});

Task("Pack")
    .IsDependentOn("Version")
    .Does<BuildState>(state => 
{
    var settings = new NuGetPackSettings
    {
        Version = state.Version.PackageVersion,
        OutputDirectory = state.Paths.OutputFolder
    };

    NuGetPack(state.Paths.NuSpecFile, settings);
});

Task("Full")
    //.IsDependentOn("BuildTemplates")
    .IsDependentOn("Pack");

RunTarget(target);


public class BuildState
{
    public VersionInfo Version { get; set; }

    public BuildPaths Paths { get; set; }
}

public class VersionInfo
{
    public string PackageVersion { get; set; }

    public string BuildVersion {get; set; }
}

public class BuildPaths
{
    public FilePath SolutionFile { get; set; }

    public FilePath NuSpecFile { get; set; }

    public DirectoryPath SolutionFolder => SolutionFile.GetDirectory();

    public DirectoryPath OutputFolder => SolutionFolder.Combine("outputs");
}
