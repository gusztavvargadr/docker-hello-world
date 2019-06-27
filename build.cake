#load "./build/core.cake"

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var buildSettings = new DockerComposeBuildSettings {
      WorkingDirectory = sourceDirectory
    };
    var service = "app";
    DockerComposeBuild(buildSettings, service);
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var runSettings = new DockerComposeRunSettings {
      WorkingDirectory = sourceDirectory
    };
    var service = "app";
    DockerComposeRun(runSettings, service);
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
    var output = workDirectory.Path + "/image.tar";
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
