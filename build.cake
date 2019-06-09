#addin "nuget:?package=Cake.Docker&version=0.10.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var dockerRegistry = "";
var dockerRepository = "gusztavvargadr/hello-world";

var version = string.Empty;

Task("Version")
  .Does(() => {
    var settings = new DockerComposeRunSettings {
    };
    var service = "gitversion";

    DockerComposeRun(settings, service);

    version = "latest";
  });

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var settings = new DockerComposeBuildSettings {
    };
    var services = new [] { "app" };

    DockerComposeBuild(settings, services);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var settings = new DockerComposeRunSettings {
    };
    var service = "app";

    DockerComposeRun(settings, service);
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
    DockerTag($"{dockerRepository}:build", $"{dockerRepository}:{version}");
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    DockerRemove(
      new DockerImageRemoveSettings(),
      new [] { $"{dockerRepository}:{version}" }
    );

    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };

    DockerComposeDown(settings);
  });

Task("Default")
  .IsDependentOn("Package");

RunTarget(target);
