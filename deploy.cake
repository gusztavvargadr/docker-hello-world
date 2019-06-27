#load "./build/core.cake"

var tags = new List<string>();

Versioned = () => {
  Environment.SetEnvironmentVariable("APP_IMAGE_REGISTRY", packageRegistry);
  Environment.SetEnvironmentVariable("APP_IMAGE_REPOSITORY", packageName);
  Environment.SetEnvironmentVariable("APP_IMAGE_TAG", $"{packageVersion}-{configuration}");

  tags.Add(packageVersion);
  tags.Add("rc");
  if (string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
    tags.Add("latest");
  }
};

Restored = () => {
  EnsureDirectoryExists(workDirectory.Path + "/registry");

  Environment.SetEnvironmentVariable("REGISTRY_VOLUME_PATH", workDirectory.Path + "/registry");

  var input = artifactsDirectory.Path + "/image.tar";
  var loadSettings = new DockerImageLoadSettings {
    Input =input
  };
  DockerLoad(loadSettings);

  var upSettings = new DockerComposeUpSettings {
    DetachedMode = true,
    WorkingDirectory = sourceDirectory
  };
  var service = "registry";
  DockerComposeUp(upSettings, service);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
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
      Environment.SetEnvironmentVariable("APP_IMAGE_TAG", $"{tag}-{configuration}");

      var pullSettings = new DockerComposePullSettings {
        WorkingDirectory = sourceDirectory
      };
      DockerComposePull(pullSettings, service);

      var runSettings = new DockerComposeRunSettings {
        WorkingDirectory = sourceDirectory
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

  foreach (var tag in tags.Skip(1)) {
    DockerRemove(removeSettings, GetDeployDockerImage(tag));
  }
};

RunTarget(target);
