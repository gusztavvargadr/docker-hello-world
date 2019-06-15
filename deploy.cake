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
    
    DockerPush(settings, GetDockerImage());
  });

Task("Test")
  .IsDependentOn("Build")
  .Does(() => {
    var removeSettings = new DockerImageRemoveSettings {
      Force = true
    };

    DockerRemove(removeSettings, GetDockerImage());

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
    DockerTag(GetDockerImage(), GetDockerImage("rc"));
    Information($"Tagged '{GetDockerImage()}' as '{GetDockerImage("rc")}'.");

    if (string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      DockerTag(GetDockerImage(), GetDockerImage("latest"));
      Information($"Tagged '{GetDockerImage()}' as '{GetDockerImage("latest")}'.");
    }
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    var settings = new DockerImagePushSettings {
    };
    
    DockerPush(settings, GetDockerImage("rc"));

    if (string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      DockerPush(settings, GetDockerImage("latest"));
    }
  });

Cleaned = () => {
  var settings = new DockerImageRemoveSettings {
    Force = true
  };

  DockerRemove(settings, GetDockerImage("rc"));
};

RunTarget(target);
