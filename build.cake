#load "tools/MK6.Tools.CakeBuild.Core/core.params.cake"

#tool nuget:?package=Grpc.Tools&version=1.0.0-pre1
#tool "nuget:?package=OctopusTools"
#tool "nuget:?package=FluentMigrator.Tools"
#tool "nuget:?package=postgresql.tools-x64"

BuildParams buildParams = BuildParams.GetParams(Context);

// Install addins.
#addin nuget:?package=MK6.Tools.CakeBuild.Grpc
#addin nuget:https://api.nuget.org/v3/index.json?package=Cake.XdtTransform

// Include Additional Cake files
#load "tools/MK6.Tools.CakeBuild.Core/core.dotnet.cake" 

Task("Clean")
    .IsDependentOn("CoreClean")
    .Does(() => {

});

Task("RestoreNuGetPackages")
    .IsDependentOn("CoreRestoreNuGetPackages")
    .Does(() =>
{
   
});

Task("Build")
    .IsDependentOn("CoreBuild")
    .Does(() =>
{
    
});

Task("Package")
    .IsDependentOn("Transform")
    .IsDependentOn("CorePackage")
    .Does(() =>
{
    
});

Task("Publish")
    .IsDependentOn("CorePublish")
    .Does(() =>
{
      
});

Task("UpdateAssemblyInfo")
    .IsDependentOn("CoreUpdateAssemblyInfo")
    .Does(() =>
{
      
});

Task("ProtoCompile")
    .Does(() => 
{
    var protoCompileSettings = new ProtoCompileSettings 
                                    { 
                                        ProtoInputFile = "protos/grpc_demo.proto", 
                                        ProtoImportPath = "protos",
                                        CSharpOutputPath = "Generated"
                                    };
    
    ProtoCompile(protoCompileSettings);
});

Task("CreateOctoRelease")
    .IsDependentOn("ProtoCompile")
    .IsDependentOn("Package")
    .Does(() =>
{
    var projectName = Argument<string>("octopusProjectName");

    OctoCreateRelease(projectName, new CreateReleaseSettings {
        Server = EnvironmentVariable("octopus_url"),
        ApiKey = EnvironmentVariable("octopus_api_key"),
        EnableServiceMessages = true,
        ReleaseNumber = buildParams.Version,
        DefaultPackageVersion = buildParams.Version,
    });
});

Task("Transform")
  .Does(() =>
  {
    var configsDir = Directory("./MK6.MicroServices.CryptoEngine/_configs");
    EnsureDirectoryExists(configsDir);
    CleanDirectories(configsDir.Path.FullPath);
    //Find all of the App.{ENVIRONMENT}.Config files and transform them...
    foreach(var file in GetFiles("./MK6.MicroServices.CryptoEngine/App.*.Config"))
    {
        Information("Transform: {0}", file);
        var env = file.GetFilename().ToString().ToLower().Replace("app.", "").Replace(".config", "");
        var sourceFile = File("./MK6.MicroServices.CryptoEngine/App.config");
        var transformFile = file;
        var envDirectory = Directory(configsDir + Directory(env));
        EnsureDirectoryExists(envDirectory);
        var targetFile = envDirectory + File("MK6.MicroServices.CryptoEngine.exe.config");
        
        XdtTransformConfig(sourceFile, transformFile, targetFile);
    }

  });

Task("Default")
  .IsDependentOn("ProtoCompile")
  .IsDependentOn("Build")
  .Does(() =>
{

});

RunTarget(buildParams.Target);
