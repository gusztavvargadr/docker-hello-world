#addin nuget:?package=Cake.Docker&version=0.11.0
#addin nuget:?package=Cake.SemVer&version=4.0.0
#addin nuget:?package=semver&version=2.0.4

var target = Argument("target", "Publish");
var configuration = Argument("configuration", "linux-amd64");
var manifestConfiguration = "manifest";

var sourceVersion = Argument("source-version", string.Empty);
Semver.SemVersion sourceSemVer;
var buildVersion = Argument("build-version", string.Empty);
var projectVersion = Argument("project-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var defaultDockerRegistry = "localhost:5000/";
var dockerRegistry = EnvironmentVariable("DOCKER_REGISTRY", defaultDockerRegistry);

var sourceRegistry = Argument("source-registry", string.Empty);
if (string.IsNullOrEmpty(sourceRegistry)) {
  sourceRegistry = dockerRegistry;
}
var packageRegistry = Argument("package-registry", string.Empty);
if (string.IsNullOrEmpty(packageRegistry)) {
  packageRegistry = dockerRegistry;
}

var dockerPlatform = configuration != manifestConfiguration ? configuration.Split('-')[0].Trim() : "linux";

Task("Init")
  .Does(() => {
    StartProcess("docker", "version");
    StartProcess("docker-compose", "version");

    Environment.SetEnvironmentVariable("DOCKER_PLATFORM", dockerPlatform);
    Information($"DOCKER_PLATFORM: '{dockerPlatform}'.");

    Environment.SetEnvironmentVariable("APP_CONFIGURATION", configuration);
    Information($"APP_CONFIGURATION: '{configuration}'.");

    EnsureDirectoryExists($"./src/{manifestConfiguration}/");
  });

Task("Version")
  .IsDependentOn("Init")
  .Does((context) => {
    if (string.IsNullOrEmpty(sourceVersion)) {
      {
        var settings = new DockerComposeUpSettings {
        };
        var services = new [] { "gitversion" };
        DockerComposeUp(WithFiles(settings), services);
      }

      {
        var runner = new GenericDockerComposeRunner<DockerComposeLogsSettings>(
          context.FileSystem,
          context.Environment,
          context.ProcessRunner,
          context.Tools
        );
        var settings = new DockerComposeLogsSettings {
          NoColor = true
        };
        var service = "gitversion";
        var output = runner.RunWithResult(
          "logs",
          WithFiles(settings),
          (items) => items.Where(item => item.Contains('|')).ToArray(),
          service
        ).Last();

        sourceVersion = output.Split('|')[1].Trim();
      }
    }
    Information($"Source version: '{sourceVersion}'.");
    sourceSemVer = ParseSemVer(sourceVersion);

    if (string.IsNullOrEmpty(buildVersion)) {
      buildVersion = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
    Information($"Build version: '{buildVersion}'.");

    if (string.IsNullOrEmpty(projectVersion)) {
      projectVersion = sourceVersion;
    }
    Information($"Project version: '{projectVersion}'.");

    if (string.IsNullOrEmpty(packageVersion)) {
      packageVersion = sourceVersion;
    }
    Information($"Package version: '{packageVersion}'.");

    var appImage = configuration != manifestConfiguration ?
      $"{sourceRegistry}hello-world:{sourceVersion}-{configuration}" :
      $"{sourceRegistry}hello-world:{sourceVersion}";
    Environment.SetEnvironmentVariable("APP_IMAGE", appImage);
    Information($"APP_IMAGE: '{appImage}'.");
  });

Task("RestoreCore")
  .IsDependentOn("Version")
  .Does(() => {
    EnsureDirectoryExists("./artifacts/registry/data/");

    if (sourceRegistry == defaultDockerRegistry) {
      var settings = new DockerComposeUpSettings {
        DetachedMode = true
      };
      var services = new [] { "registry" };
      DockerComposeUp(WithFiles(settings), services);
    }
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };
    DockerComposeDown(WithFiles(settings));
  });

private string GetAppImageReference() => EnvironmentVariable("APP_IMAGE");

private T WithFiles<T>(T settings) where T : DockerComposeSettings {
  settings.Files = new [] {
    "./docker-compose.yml",
    $"./build/docker/{dockerPlatform}/docker-compose.override.yml"
  };

  return settings;
}
