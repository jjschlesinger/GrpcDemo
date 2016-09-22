#addin "Cake.FileHelpers"

var msBuildSettings = new MSBuildSettings()
                            .SetConfiguration(buildParams.Configuration)
                            .SetVerbosity(Verbosity.Minimal)
                            .UseToolVersion(MSBuildToolVersion.VS2015);

Task("CoreClean")
    .Does(() => 
    {
        MSBuild(buildParams.SolutionPath, msBuildSettings.WithTarget("Clean"));
        DeleteFiles("./*.nupkg");
    });

Task("CoreRestoreNuGetPackages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    if(HasEnvironmentVariable("NUGET_SOURCES")) 
    {
        var sources = new List<string>(EnvironmentVariable("NUGET_SOURCES").Split(';'));
        NuGetRestore(buildParams.SolutionPath, new NuGetRestoreSettings { Source = sources });
    }
    else
        NuGetRestore(buildParams.SolutionPath);
    
});

Task("CoreBuild")
    .IsDependentOn("RestoreNuGetPackages")
    .IsDependentOn("UpdateAssemblyInfo")
    .Does(() => MSBuild(buildParams.SolutionPath, msBuildSettings.WithTarget("Build")));

Task("CorePackage")
    .IsDependentOn("Build")
    .Does(() =>
{
    foreach(var nuspec in buildParams.NuspecPaths)
    {
        NuGetPack(nuspec.FullPath, new NuGetPackSettings {
                                    Version = buildParams.Version,
                                    Properties = new Dictionary<string, string>{ {"configuration", buildParams.Configuration} },
                                    NoPackageAnalysis = true
        });
    }
});

Task("CorePublish")
    .IsDependentOn("Package")
    .Does(() =>
{
    var parsedSolution = ParseSolution(buildParams.SolutionPath);
    var nupkgs = GetFiles("./*.nupkg").Where(fi => parsedSolution.Projects.Any(p => string.Format("{0}.{1}.nupkg", p.Name, buildParams.Version) == fi.GetFilename().ToString()));
    foreach(var nupkg in nupkgs)
    {
        NuGetPush(nupkg.FullPath, new NuGetPushSettings {
            ApiKey = EnvironmentVariable("NUGET_PUSH_API_KEY"),
            Source = EnvironmentVariable("NUGET_PUSH_SOURCE")
        });
    }
    
});

Task("CoreUpdateAssemblyInfo")
    .Does(() =>
{
    var companyName = EnvironmentVariable("ASSEMBLY_INFO_COMPANY");

    FilePath[] replacedFiles = null;
    replacedFiles = ReplaceRegexInFiles("./**/AssemblyInfo.cs", @"AssemblyVersion[(][""](.*)[""][)]", String.Format("AssemblyVersion(\"{0}\")", buildParams.Version));
    Information("Updated assembly version in the following files: " + string.Join("; ", replacedFiles.Select(x => x.FullPath)));
    replacedFiles = ReplaceRegexInFiles("./**/AssemblyInfo.cs", @"AssemblyFileVersion[(][""](.*)[""][)]", String.Format("AssemblyFileVersion(\"{0}\")", buildParams.Version));
    Information("Updated assembly file version in the following files: " + string.Join("; ", replacedFiles.Select(x => x.FullPath)));
    
    if(!string.IsNullOrEmpty(companyName))
    {
        replacedFiles = ReplaceRegexInFiles("./**/AssemblyInfo.cs", @"AssemblyCompany[(][""](.*)[""][)]", String.Format("AssemblyCompany(\"{0}\")", companyName));
        Information("Updated assembly company name in the following files: " + string.Join("; ", replacedFiles.Select(x => x.FullPath)));
        replacedFiles = ReplaceRegexInFiles("./**/AssemblyInfo.cs", @"AssemblyCopyright[(][""](.*)[""][)]", string.Format("AssemblyCopyright(\"Copyright (c) {1} {0}\")", DateTime.Now.Year, companyName));
        Information("Updated assembly copyright in the following files: " + string.Join("; ", replacedFiles.Select(x => x.FullPath)));
    }
    
});