#load "./build/core.cake"

Versioned = () => {
  Environment.SetEnvironmentVariable("APP_IMAGE_REGISTRY", packageRegistry);
  Environment.SetEnvironmentVariable("APP_IMAGE_REPOSITORY", packageName);
  Environment.SetEnvironmentVariable("APP_IMAGE_TAG", packageVersion);
};

Restored = () => {
  var input = artifactsDirectory.Path + $"/{sourceVersion}.tar";
  var loadSettings = new DockerImageLoadSettings {
    Input =input
  };
  DockerLoad(loadSettings);

  var upSettings = new DockerComposeUpSettings {
    DetachedMode = true
  };
  var service = "registry";
  DockerComposeUp(upSettings, service);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    DockerTag(GetBuildDockerImage(), GetDeployDockerImage());

    if (packageVersion == "latest" && !string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      Information($"Skipping pushing '{GetDeployDockerImage()}'.");
    } else {
      var pushSettings = new DockerImagePushSettings {
      };
      DockerPush(pushSettings, GetDeployDockerImage());
    }
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var service = "app";

    if (packageVersion == "latest" && !string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      Information($"Skipping pulling '{GetDeployDockerImage()}'.");
    } else {
      var removeSettings = new DockerImageRemoveSettings {
        Force = true
      };
      DockerRemove(removeSettings, GetDeployDockerImage());

      var pullSettings = new DockerComposePullSettings {
      };
      DockerComposePull(pullSettings, service);
    }

    var runSettings = new DockerComposeRunSettings {
    };
    DockerComposeRun(runSettings, service);
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
};

RunTarget(target);
