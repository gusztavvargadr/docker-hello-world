#load "core.cake"

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
    DockerTag(GetDockerImageSource(), GetDockerImageTarget());

    if (packageVersion == "latest" && !string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      Information($"Skipping pushing '{GetDockerImageTarget()}'.");
    } else {
      var pushSettings = new DockerImagePushSettings {
      };
      DockerPush(pushSettings, GetDockerImageTarget());
    }
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var service = "app";

    if (packageVersion == "latest" && !string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      Information($"Skipping pulling '{GetDockerImageTarget()}'.");
    } else {
      var removeSettings = new DockerImageRemoveSettings {
        Force = true
      };
      DockerRemove(removeSettings, GetDockerImageTarget());

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
  DockerRemove(removeSettings, GetDockerImageSource());
};

RunTarget(target);
