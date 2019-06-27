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
  if (configuration != "manifest") {
    GZipUncompress(artifactsDirectory.Path + "/image.tar.gz", workDirectory);

    var input = workDirectory.Path + "/image.tar";
    var loadSettings = new DockerImageLoadSettings {
      Input =input
    };
    DockerLoad(loadSettings);
  }

  EnsureDirectoryExists(workDirectory.Path + "/registry");
  Environment.SetEnvironmentVariable("REGISTRY_VOLUME_PATH", MakeAbsolute(workDirectory) + "/registry");

  if (configuration == "manifest") {
    foreach (var file in GetFiles(artifactsDirectory.Path + "/../**/registry.tar.gz")) {
      GZipUncompress(file, workDirectory.Path + "/registry");
    }
  }

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
      if (configuration != "manifest") {
        DockerTag(GetBuildDockerImage(), GetDeployDockerImage(tag));

        var pushSettings = new DockerImagePushSettings {
        };
        DockerPush(pushSettings, GetDeployDockerImage(tag));
      } else {
        var createCommand = $"manifest create --insecure --amend {packageRegistry}{packageName}:{tag}";
        foreach (var directory in GetDirectories(artifactsDirectory.Path + "/../*")) {
          if (directory.GetDirectoryName() == "manifest") {
            continue;
          }

          createCommand += $" {packageRegistry}{packageName}:{tag}-{directory.GetDirectoryName()}";
        }
        DockerCustomCommand(createCommand);

        var pushCommand = $"manifest push --insecure {packageRegistry}{packageName}:{tag}";
        DockerCustomCommand(pushCommand);
      }
    }
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var service = "app";

    foreach (var tag in tags) {
      if (configuration != "manifest") {
        Environment.SetEnvironmentVariable("APP_IMAGE_TAG", $"{tag}-{configuration}");
      } else {
        Environment.SetEnvironmentVariable("APP_IMAGE_TAG", $"{tag}");
      }

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
    if (configuration != "manifest") {
      GZipCompress(workDirectory.Path + "/registry", artifactsDirectory.Path + "/registry.tar.gz");
    }
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
