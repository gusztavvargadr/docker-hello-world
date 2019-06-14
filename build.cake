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
    var settings = new DockerImageSaveSettings {
      Output = buildDirectory.Path + $"/{sourceVersion}.tar"
    };

    DockerSave(settings, GetDockerImage());
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    CopyFiles(buildDirectory.Path + "/**/*.tar", artifactsDirectory);
  });

RunTarget(target);
