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
    var pushSettings = new DockerImagePushSettings {
    };
    DockerPush(pushSettings, GetDockerImageTarget());
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var removeSettings = new DockerImageRemoveSettings {
      Force = true
    };
    DockerRemove(removeSettings, GetDockerImageTarget());

    var service = "app";

    var pullSettings = new DockerComposePullSettings {
    };
    DockerComposePull(pullSettings, service);

    var runSettings = new DockerComposeRunSettings {
    };
    DockerComposeRun(runSettings, service);
  });

Task("Package")
  .IsDependentOn("Test")
  .Does(() => {
    DockerTag(GetDockerImageSource(), GetDockerImageTarget("rc"));

    Information($"Tagged '{GetDockerImageSource()}' as '{GetDockerImageTarget("rc")}'.");

    if (string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      DockerTag(GetDockerImageSource(), GetDockerImageTarget("latest"));

      Information($"Tagged '{GetDockerImageSource()}' as '{GetDockerImageTarget("latest")}'.");
    }
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    var pushSettings = new DockerImagePushSettings {
    };

    DockerPush(pushSettings, GetDockerImageTarget("rc"));

    if (string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      DockerPush(pushSettings, GetDockerImageTarget("latest"));
    }
  });

Cleaned = () => {
  var removeSettings = new DockerImageRemoveSettings {
    Force = true
  };
  DockerRemove(removeSettings, GetDockerImageTarget("rc"));
};

RunTarget(target);
