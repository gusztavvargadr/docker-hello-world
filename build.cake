#addin "nuget:?package=Cake.Docker&version=0.10.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var version = Argument("app-version", string.Empty);

var dockerRepository = "gusztavvargadr/hello-world";

Task("Version")
  .Does(context => {
    try {
      if (!string.IsNullOrEmpty(version)) {
        return;
      }

      var settings = new DockerComposeUpSettings {
      };
      var service = "gitversion";

      DockerComposeUp(settings, service);

      var logs = DockerComposeLogs(context, new DockerComposeLogsSettings { NoColor = true }, service);
      version = logs.Split(Environment.NewLine).Last().Split('|').Last().Trim();
    } finally {
      Information(version);
      Environment.SetEnvironmentVariable("APP_IMAGE_TAG", version);
    }
  });

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    Environment.SetEnvironmentVariable("APP_IMAGE_TAG", version);

    var settings = new DockerComposePullSettings {
      IgnorePullFailures = true
    };
    var services = new [] { "app" };

    DockerComposePull(settings, services);
  });

Task("Build")
  .IsDependentOn("Restore")
  .Does(() => {
    var settings = new DockerComposeBuildSettings {
    };
    var services = new [] { "app" };

    DockerComposeBuild(settings, services);
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
  });

Task("Publish")
  .IsDependentOn("Package")
  .Does(() => {
    var settings = new DockerImagePushSettings {
    };
    
    DockerPush(settings, $"{dockerRepository}:{version}");
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };

    DockerComposeDown(settings);
  });

Task("Default")
  .IsDependentOn("Package");

private string DockerComposeLogs(ICakeContext context, DockerComposeLogsSettings settings, string service) {
  var runner = new GenericDockerComposeRunner<DockerComposeLogsSettings>(
    context.FileSystem,
    context.Environment,
    context.ProcessRunner,
    context.Tools
  );

  var output = runner.RunWithResult<string>("logs", settings, (processOutput) => processOutput.ToArray(), service);

  return string.Join(Environment.NewLine, output);
}

RunTarget(target);
