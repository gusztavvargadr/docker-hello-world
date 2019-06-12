#load "core.cake"

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

    if (string.IsNullOrEmpty(semanticVersion.Prerelease)) {
      DockerTag(GetDockerImage(), GetDockerImage("latest"));
    }
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    var settings = new DockerImagePushSettings {
    };
    
    DockerPush(settings, GetDockerImage("rc"));

    if (string.IsNullOrEmpty(semanticVersion.Prerelease)) {
      DockerPush(settings, GetDockerImage("latest"));
    }
  });

Task("Default")
  .IsDependentOn("Package");

RunTarget(target);
