#load "./build/core.cake"

Versioned = () => {
  Environment.SetEnvironmentVariable("APP_IMAGE_REGISTRY", defaultPackageRegistry);
  Environment.SetEnvironmentVariable("APP_IMAGE_REPOSITORY", packageName);
  Environment.SetEnvironmentVariable("APP_IMAGE_TAG", sourceVersion);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var buildSettings = new DockerComposeBuildSettings {
    };
    var service = "app";
    DockerComposeBuild(buildSettings, service);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var runSettings = new DockerComposeRunSettings {
    };
    var service = "app";
    DockerComposeRun(runSettings, service);
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
    var output = workDirectory.Path + $"/{sourceVersion}.tar";
    var saveSettings = new DockerImageSaveSettings {
      Output = output
    };
    DockerSave(saveSettings, GetBuildDockerImage());

    Information($"Saved '{output}'.");
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    CopyFiles(workDirectory.Path + "/**/*.tar", artifactsDirectory);

    Information($"Copied artifacts to '{artifactsDirectory}'.");
  });

RunTarget(target);
