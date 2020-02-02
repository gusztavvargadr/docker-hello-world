#load ./build/cake/core.cake

Task("Restore")
  .IsDependentOn("RestoreCore")
  .Does(() => {
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    if (configuration != manifestConfiguration) {
      var settings = new DockerComposeBuildSettings {
      };
      var services = new [] { "app" };
      DockerComposeBuild(WithFiles(settings), services);
    }
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    if (configuration != manifestConfiguration) {
      var settings = new DockerComposeRunSettings {
      };
      var service = "app";
      DockerComposeRun(WithFiles(settings), service);
    }
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    if (configuration != manifestConfiguration) {
      var settings = new DockerImagePushSettings {
      };
      var imageReference = GetAppImageReference();
      DockerPush(settings, imageReference);
    }
  });

RunTarget(target);
