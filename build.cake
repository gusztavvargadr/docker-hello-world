#addin "nuget:?package=Cake.Docker&version=0.10.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var version = Argument("app-version", string.Empty);

var dockerRegistry = "";
var dockerRepository = "gusztavvargadr/hello-world";

Task("Version")
  .WithCriteria(() => string.IsNullOrEmpty(version))
  .Does(() => {
    var settings = new DockerComposeRunSettings {
    };
    var service = "gitversion";

    DockerComposeRun(settings, service);

    version = "gitversion";
  });

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposePullSettings {
      IgnorePullFailures = true
    };
    var services = new [] { "app" };

    DockerComposePull(settings, services);
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
    DockerTag($"{dockerRepository}:ci", $"{dockerRepository}:{version}");
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    var settings = new DockerImagePushSettings {
    };
    
    DockerPush(settings, $"{dockerRepository}:ci");
    DockerPush(settings, $"{dockerRepository}:{version}");
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    DockerRemove(
      new DockerImageRemoveSettings { Force = true },
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
