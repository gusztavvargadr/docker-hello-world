#load "core.cake"

Restored = () => {
  var input = artifactsDirectory.Path + $"/{sourceVersion}.tar";
  var settings = new DockerImageLoadSettings {
    Input =input
  };

  DockerLoad(settings);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var settings = new DockerImagePushSettings {
    };

    DockerTag(GetDockerImageSource(), GetDockerImageTarget("rc"));
    DockerPush(settings, GetDockerImageTarget());
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
    var settings = new DockerImagePushSettings {
    };
    
    DockerPush(settings, GetDockerImageTarget("rc"));

    if (string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      DockerPush(settings, GetDockerImageTarget("latest"));
    }
  });

Cleaned = () => {
  var settings = new DockerImageRemoveSettings {
    Force = true
  };

  DockerRemove(settings, GetDockerImageTarget("rc"));
};

RunTarget(target);
