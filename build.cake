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
    var output = buildDirectory.Path + $"/{sourceVersion}.tar";
    var settings = new DockerImageSaveSettings {
      Output = output
    };

    DockerSave(settings, GetDockerImageSource());

    Information($"Saved '{output}'.");
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    CopyFiles(buildDirectory.Path + "/**/*.tar", artifactsDirectory);

    Information($"Copied to '{artifactsDirectory}'.");
  });

RunTarget(target);
