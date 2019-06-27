#addin "nuget:?package=Cake.SemVer&version=3.0.0"
#addin "nuget:?package=semver&version=2.0.4"
#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.Compression&version=0.2.3"
#addin "nuget:?package=SharpZipLib&version=1.1.0"

var target = Argument("target", "Publish");
var configuration = Argument("configuration", "linux-amd64");

var sourceDirectory = Directory($"{Argument("source-directory", "./src")}/{configuration}");
var workDirectory = Directory($"{Argument("work-directory", "./work")}/{configuration}");
var artifactsDirectory = Directory($"{Argument("artifacts-directory", "./artifacts")}/{configuration}");

var sourceVersion = Argument("source-version", string.Empty);
Semver.SemVersion sourceSemVer;
var buildVersion = Argument("build-version", string.Empty);
var appVersion = Argument("app-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var packageRegistry = Argument("package-registry", "localhost:5000/");
var packageName = Argument("package-name", "gusztavvargadr/hello-world");

Task("Version")
  .Does(() => {
    sourceVersion = !string.IsNullOrEmpty(sourceVersion) ? sourceVersion : GetSourceVersion();
    sourceSemVer = ParseSemVer(sourceVersion);
    buildVersion = !string.IsNullOrEmpty(buildVersion) ? buildVersion : GetBuildVersion();
    appVersion = !string.IsNullOrEmpty(appVersion) ? appVersion : GetAppVersion();
    packageVersion = !string.IsNullOrEmpty(packageVersion) ? packageVersion : GetPackageVersion();

    Information($"Source: '{sourceVersion}'.");
    Information($"Build: '{buildVersion}'.");
    Information($"App: '{appVersion}'.");
    Information($"Package: '{packageVersion}'.");

    Versioned();
  });

private string GetSourceVersion() {
  using(var process = StartAndReturnProcess(
    "dotnet",
    new ProcessSettings {
      Arguments = $"gitversion /showvariable SemVer",
      RedirectStandardOutput = true
    }
  )) {
    process.WaitForExit();
    if (process.GetExitCode() != 0) {
      throw new Exception($"Error executing 'GitVersion': '{process.GetExitCode()}'.");
    }

    return string.Join(Environment.NewLine, process.GetStandardOutput());
  }
}

Func<string> GetBuildVersion = () => {
  return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
};

Func<string> GetAppVersion = () => {
  return sourceVersion;
};

Func<string> GetPackageVersion = () => {
  return GetAppVersion();
};

Action Versioned = () => {};

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    EnsureDirectoryExists(workDirectory);
    EnsureDirectoryExists(artifactsDirectory);

    Restored();
  });

Action Restored = () => {};

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var downSettings = new DockerComposeDownSettings {
      Rmi = "all",
      WorkingDirectory = sourceDirectory
    };
    DockerComposeDown(downSettings);

    CleanDirectory(workDirectory);

    Cleaned();
  });

Action Cleaned = () => {};

private string GetBuildDockerImage() {
  return $"local/{packageName}:build-{configuration}";
}

private string GetDeployDockerImage(string alias) {
  return $"{packageRegistry}{packageName}:{alias}-{configuration}";
}
