#addin "nuget:?package=Cake.Docker&version=0.10.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var version = Argument("build-version", string.Empty);

var dockerRegistry = Argument("docker-registry", string.Empty);
var dockerRepository = Argument("docker-repository", "gusztavvargadr/hello-world");

Task("Version")
  .Does(context => {
    try {
      Environment.SetEnvironmentVariable("APP_IMAGE_REPOSITORY", dockerRepository);
      Environment.SetEnvironmentVariable("APP_IMAGE_REGISTRY", dockerRegistry);

      if (!string.IsNullOrEmpty(version)) {
        return;
      }

      var settings = new DockerComposeUpSettings {
      };
      var service = "gitversion";

      DockerComposeUp(settings, service);

      var output = DockerComposeLogs(context, new DockerComposeLogsSettings { NoColor = true }, service);
      version = output.Split(Environment.NewLine).Last().Split('|').Last().Trim().Replace("-rc-origin-", "-rc-");
    } finally {
      Information(version);

      Environment.SetEnvironmentVariable("APP_IMAGE_TAG", version);
    }
  });

Task("Clean")
  .IsDependentOn("Version")
  .Does(() => {
    var settings = new DockerComposeDownSettings {
      Rmi = "all"
    };

    DockerComposeDown(settings);
  });

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

private string GetDockerImage(string tag = null) {
  if (string.IsNullOrEmpty(tag)) {
    tag = version;
  }

  return $"{dockerRegistry}{dockerRepository}:{tag}";
}
