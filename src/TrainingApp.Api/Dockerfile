# Build from the repository root, where Directory.*.props and global.json live:
#   docker build -f src/TrainingApp.Api/Dockerfile -t trainingapp-api .
# The e2e tests (tests/TrainingApp.E2ETests) build this image with Testcontainers.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore first, in its own layer: it is re-run only when the project or package versions change.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/TrainingApp.Api/TrainingApp.Api.csproj src/TrainingApp.Api/
RUN dotnet restore src/TrainingApp.Api/TrainingApp.Api.csproj

COPY src/TrainingApp.Api/ src/TrainingApp.Api/
RUN dotnet publish src/TrainingApp.Api/TrainingApp.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# The aspnet image listens on 8080 and runs as the non-root "app" user.
EXPOSE 8080
ENTRYPOINT ["dotnet", "TrainingApp.Api.dll"]
