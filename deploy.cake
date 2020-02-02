#load ./build/cake/core.cake

Task("Restore")
  .IsDependentOn("RestoreCore")
  .Does(() => {
    if (configuration != manifestConfiguration) {
      var settings = new DockerImagePullSettings {
      };
      var imageReference = GetAppImageReference();
      DockerPull(settings, imageReference);
    }

    if (packageRegistry == defaultDockerRegistry) {
      var settings = new DockerComposeUpSettings {
        DetachedMode = true
      };
      var services = new [] { "registry" };
      DockerComposeUp(WithFiles(settings), services);
    }
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
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
    if (configuration != manifestConfiguration) {
      var imageReference = GetAppImageReference();

      var appImage = configuration != manifestConfiguration ?
        $"{packageRegistry}hello-world:{packageVersion}-{configuration}" :
        $"{packageRegistry}hello-world:{packageVersion}";
      Environment.SetEnvironmentVariable("APP_IMAGE", appImage);
      Information($"APP_IMAGE: '{appImage}'.");

      var registryReference = GetAppImageReference();

      Information($"Tagging '{imageReference}' as '{registryReference}'.");
      DockerTag(imageReference, registryReference);
    } else {
      var appImage = configuration != manifestConfiguration ?
        $"{packageRegistry}hello-world:{packageVersion}-{configuration}" :
        $"{packageRegistry}hello-world:{packageVersion}";
      Environment.SetEnvironmentVariable("APP_IMAGE", appImage);
      Information($"APP_IMAGE: '{appImage}'.");

      var settings = new DockerManifestCreateSettings() {
        Insecure = true,
        Amend = true
      };
      var manifestList = GetAppImageReference();
      var manifestTags = new [] { "linux-amd64", "windows-amd64" };
      foreach (var manifestTag in manifestTags) {
        var manifest = $"{manifestList}-{manifestTag}";
        DockerManifestCreate(settings, manifestList, manifest);
      }
    }
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    if (configuration != manifestConfiguration) {
      var settings = new DockerImagePushSettings {
      };
      var imageReference = GetAppImageReference();
      DockerPush(settings, imageReference);
    } else {
      var settings = new DockerManifestPushSettings() {
        Insecure = true,
        Purge = true
      };
      var manifestList = GetAppImageReference();
      DockerManifestPush(settings, manifestList);
    }
  });

RunTarget(target);
