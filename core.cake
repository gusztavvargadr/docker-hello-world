#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.SemVer&version=3.0.0"
#addin "nuget:?package=semver&version=2.0.4"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var sourceVersion = Argument("source-version", string.Empty);
Semver.SemVersion sourceSemVer;
var buildVersion = Argument("build-version", string.Empty);
var appVersion = Argument("app-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var sourceDirectory = Directory(Argument("source-directory", "./src"));
var buildDirectory = Directory(Argument("build-directory", "./build"));
var artifactsDirectory = Directory(Argument("artifacts-directory", "./artifacts"));

var dockerRegistry = Argument("docker-registry", string.Empty);
var dockerRepository = Argument("docker-repository", "gusztavvargadr/hello-world");

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
        appVersion = new Semver.SemVersion(sourceSemVer.Major, sourceSemVer.Minor, sourceSemVer.Patch).ToString();
      }
      if (string.IsNullOrEmpty(packageVersion)) {
        packageVersion = appVersion;
      }

      Information($"Source: '{sourceVersion}'.");
      Information($"Build: '{buildVersion}'.");
      Information($"App: '{appVersion}'.");
      Information($"Package: '{packageVersion}'.");

      Environment.SetEnvironmentVariable("APP_IMAGE_REPOSITORY", dockerRepository);
      Environment.SetEnvironmentVariable("APP_IMAGE_REGISTRY", dockerRegistry);
      Environment.SetEnvironmentVariable("APP_IMAGE_TAG", sourceVersion);
    }
  });

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposePullSettings {
      IgnorePullFailures = true
    };

    DockerComposePull(settings);

    EnsureDirectoryExists(buildDirectory);

    EnsureDirectoryExists(artifactsDirectory);
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };

    DockerComposeDown(settings);

    CleanDirectory(artifactsDirectory);

    CleanDirectory(buildDirectory);
  });

Task("Default")
  .IsDependentOn("Publish");

private string GetDockerImage(string tag = null) {
  if (string.IsNullOrEmpty(tag)) {
    tag = sourceVersion;
  }

  return $"{dockerRegistry}{dockerRepository}:{tag}";
}
