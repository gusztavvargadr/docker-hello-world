#load "./build/core.cake"

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
    if (string.IsNullOrEmpty(sourceSemVer.Prerelease)) {
      tags.Add("latest");
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
      };
      DockerComposePull(pullSettings, service);

      var runSettings = new DockerComposeRunSettings {
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
