#load "core.cake"

Restored = () => {
  var settings = new DockerImageLoadSettings {
    Input = artifactsDirectory.Path + $"/{sourceVersion}.tar"
  };

  DockerLoad(settings);
};

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
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
    
    DockerPush(settings, GetDockerImage());

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
