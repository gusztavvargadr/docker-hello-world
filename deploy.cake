#load "./build/core.cake"

Restored = () => {
  EnsureDirectoryExists(workDirectory.Path + "/registry");

  var input = artifactsDirectory.Path + $"/{sourceVersion}-{configuration}.tar";
  var loadSettings = new DockerImageLoadSettings {
    Input =input
  };
  DockerLoad(loadSettings);

  var upSettings = new DockerComposeUpSettings {
    DetachedMode = true,
    WorkingDirectory = workDirectory
  };
  var service = "registry";
  DockerComposeUp(upSettings, service);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    if (string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      tags.Add($"latest-{configuration}");
    }

    foreach (var tag in tags) {
      DockerTag(GetBuildDockerImage(), GetDeployDockerImage(tag));

      var pushSettings = new DockerImagePushSettings {
      };
      DockerPush(pushSettings, GetDeployDockerImage(tag));
    }
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var service = "app";

    foreach (var tag in tags) {
      Environment.SetEnvironmentVariable("APP_IMAGE_TAG", tag);

      var pullSettings = new DockerComposePullSettings {
        WorkingDirectory = workDirectory
      };
      DockerComposePull(pullSettings, service);

      var runSettings = new DockerComposeRunSettings {
        WorkingDirectory = workDirectory
      };
      DockerComposeRun(runSettings, service);
    }
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
  });

Cleaned = () => {
  var removeSettings = new DockerImageRemoveSettings {
    Force = true
  };
  DockerRemove(removeSettings, GetBuildDockerImage());

  foreach (var tag in tags) {
    DockerRemove(removeSettings, GetDeployDockerImage(tag));
  }
};

RunTarget(target);
