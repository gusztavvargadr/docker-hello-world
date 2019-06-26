#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.SemVer&version=3.0.0"
#addin "nuget:?package=semver&version=2.0.4"

var target = Argument("target", "Publish");
var configuration = Argument("configuration", "linux-amd64");

var sourceVersion = Argument("source-version", string.Empty);
Semver.SemVersion sourceSemVer;
var buildVersion = Argument("build-version", string.Empty);
var appVersion = Argument("app-version", string.Empty);
var packageVersion = Argument("package-version", string.Empty);

var sourceDirectory = Directory(Argument("source-directory", "./src"));
var workDirectory = Directory(Argument("work-directory", "./work"));
var artifactsDirectory = Directory(Argument("artifacts-directory", "./artifacts"));

var packageName = Argument("package-name", "gusztavvargadr/hello-world");
var defaultPackageRegistry = "localhost:5000/";
var packageRegistry = Argument("package-registry", defaultPackageRegistry);

var tags = new List<string>();

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
  return $"{sourceVersion}-{configuration}";
};

Func<string> GetPackageVersion = () => {
  return GetAppVersion();
};

Action Versioned = () => {
  Environment.SetEnvironmentVariable("APP_IMAGE_REGISTRY", packageRegistry);
  Environment.SetEnvironmentVariable("APP_IMAGE_REPOSITORY", packageName);
  Environment.SetEnvironmentVariable("APP_IMAGE_TAG", packageVersion);

  tags.Add(packageVersion);
  tags.Add($"rc-{configuration}");
};

Task("Restore")
  .IsDependentOn("Version")
  .Does(() => {
    EnsureDirectoryExists(workDirectory);
    EnsureDirectoryExists(artifactsDirectory);

    CopyFiles(sourceDirectory.Path + $"/{configuration}/**/*.*", workDirectory);

    Restored();
  });

Action Restored = () => {};

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var downSettings = new DockerComposeDownSettings {
      Rmi = "all",
      WorkingDirectory = workDirectory
    };
    DockerComposeDown(downSettings);

    CleanDirectory(workDirectory);

    Cleaned();
  });

Action Cleaned = () => {};

private string GetBuildDockerImage() {
  return $"{defaultPackageRegistry}{packageName}:{packageVersion}";
}

private string GetDeployDockerImage(string tag) {
  return $"{packageRegistry}{packageName}:{tag}";
}
