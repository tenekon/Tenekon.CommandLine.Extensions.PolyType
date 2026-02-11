import { dag, Container, Directory, object, func, Client, connect } from "@dagger.io/dagger"

const DOTNET_IMAGE = "mcr.microsoft.com/dotnet/sdk:10.0";
const COVERAGE_DIR = "TestResults/coverage/";
const COVERAGE_FILE = `${COVERAGE_DIR}coverage.cobertura.xml`;

@object()
export class TenekonCommandLineExtensionsPolyType {
  @func()
  hello(shout: boolean): string {
    const message = "Hello, world"
    if (shout) {
      return message.toUpperCase()
    }
    return message
  }

  @func()
  uploadDotnetTestCoverage(src: Directory, codecovToken: string): Container {
    return dag
        .container()
        .from(DOTNET_IMAGE)
        .withMountedDirectory("/src", src)
        .withWorkdir("/src")
        .withEnvVariable("DOTNET_NOLOGO", "1")
        .withEnvVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
        .withExec(["dotnet", "restore"])
        .withExec([
          "dotnet",
          "test",
          "--nologo",
          "/p:CollectCoverage=true",
          "/p:CoverletOutputFormat=cobertura",
          `/p:CoverletOutput=${COVERAGE_DIR}`
        ])
        .withEnvVariable("CODECOV_TOKEN", codecovToken)
        .withExec([
          "sh",
          "-c",
          "curl -s https://uploader.codecov.io/latest/linux/codecov -o /usr/local/bin/codecov"
        ])
        .withExec(["chmod", "+x", "/usr/local/bin/codecov"])
        .withExec(["/usr/local/bin/codecov", "-f", COVERAGE_FILE]);
  }
}
