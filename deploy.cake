#load "core.cake"

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposePullSettings {
      IgnorePullFailures = false
    };

    DockerComposePull(settings);
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
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
    DockerTag(GetDockerImage(), GetDockerImage("rc"));
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    var settings = new DockerImagePushSettings {
    };
    
    DockerPush(settings, GetDockerImage("rc"));
  });

Task("Default")
  .IsDependentOn("Package");

RunTarget(target);
