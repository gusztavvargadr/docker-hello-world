#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.SemVer&version=3.0.0"
#addin "nuget:?package=semver&version=2.0.4"

var target = Argument("target", "Publish");
var configuration = Argument("configuration", "Release");

var sourceVersion = Argument("source-version", string.Empty);
Semver.SemVersion sourceSemVer;
var buildVersion = Argument("build-version", string.Empty);
var appVersion = Argument("app-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var sourceDirectory = Directory(Argument("source-directory", "./src"));
var buildDirectory = Directory(Argument("build-directory", "./build"));
var artifactsDirectory = Directory(Argument("artifacts-directory", "./artifacts"));

var dockerRegistryDefault = "localhost:5000";
var dockerRegistrySource = $"{Argument("docker-registry-source", dockerRegistryDefault)}/";
var dockerRegistryTarget = $"{Argument("docker-registry-target", dockerRegistryDefault)}/";
var dockerRepository = Argument("docker-repository", "gusztavvargadr/hello-world");

Action Versioned = () => {
  Environment.SetEnvironmentVariable("APP_IMAGE_REGISTRY", dockerRegistryTarget);
  Environment.SetEnvironmentVariable("APP_IMAGE_REPOSITORY", dockerRepository);
  Environment.SetEnvironmentVariable("APP_IMAGE_TAG", packageVersion);
};

Task("Version")
  .Does(context => {
    try {
      if (!string.IsNullOrEmpty(sourceVersion)) {
        return;
      }

      using(var process = StartAndReturnProcess(
        "dotnet",
        new ProcessSettings {
          Arguments = $"gitversion {context.Environment.WorkingDirectory} /showvariable SemVer",
          RedirectStandardOutput = true
        }
      )) {
        process.WaitForExit();
        if (process.GetExitCode() != 0) {
          throw new Exception($"Error executing 'GitVersion': '{process.GetExitCode()}'.");
        }

        sourceVersion = string.Join(Environment.NewLine, process.GetStandardOutput());
      }
    } finally {
      sourceSemVer = ParseSemVer(sourceVersion);

      if (string.IsNullOrEmpty(buildVersion)) {
        buildVersion = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
      }
      if (string.IsNullOrEmpty(appVersion)) {
        appVersion = sourceVersion;
      }
      if (string.IsNullOrEmpty(packageVersion)) {
        packageVersion = sourceVersion;
      }

      Information($"Source: '{sourceVersion}'.");
      Information($"Build: '{buildVersion}'.");
      Information($"App: '{appVersion}'.");
      Information($"Package: '{packageVersion}'.");

      Versioned();
    }
  });

Action Restored = () => {};

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    EnsureDirectoryExists(buildDirectory);
    EnsureDirectoryExists(artifactsDirectory);

    Restored();
  });

Action Cleaned = () => {};

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var downSettings = new DockerComposeDownSettings {
      Rmi = "all"
    };
    DockerComposeDown(downSettings);

    CleanDirectory(artifactsDirectory);
    CleanDirectory(buildDirectory);

    Cleaned();
  });

private string GetDockerImageSource(string tag = null) {
  if (string.IsNullOrEmpty(tag)) {
    tag = sourceVersion;
  }

  return $"{dockerRegistrySource}{dockerRepository}:{tag}";
}

private string GetDockerImageTarget(string tag = null) {
  if (string.IsNullOrEmpty(tag)) {
    tag = packageVersion;
  }

  return $"{dockerRegistryTarget}{dockerRepository}:{tag}";
}
