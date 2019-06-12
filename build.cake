#load "core.cake"

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var settings = new DockerComposeBuildSettings {
    };
    var service = "app";

    DockerComposeBuild(settings, service);
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
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    var settings = new DockerImagePushSettings {
    };
    
    DockerPush(settings, GetDockerImage());
  });

Task("Default")
  .IsDependentOn("Package");

RunTarget(target);
