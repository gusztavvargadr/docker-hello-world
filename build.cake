#addin "nuget:?package=Cake.Docker&version=0.10.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var sourceDirectory = Argument("sourceDirectory", "src");

var dockerRegistry = "";
var dockerRepository = "gusztavvargadr/hello-world";

Task("Version")
  .Does(() => {
    var settings = new DockerComposeRunSettings {
      Rm = true
    };
    var service = $"gitversion";

    DockerComposeRun(settings, service);
  });

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var settings = new DockerImageBuildSettings {
      Tag = new [] { $"{dockerRepository}:build" }
    };
    var path = sourceDirectory;

    DockerBuild(settings, path);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var settings = new DockerContainerRunSettings {
      Rm = true
    };
    var image = $"{dockerRepository}:build";
    var command = string.Empty;

    DockerRunWithoutResult(settings, image, command);
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
  });

Task("Default")
  .IsDependentOn("Package");

RunTarget(target);
