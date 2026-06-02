# KnowledgeOps-AI Worker - Multi-stage Dockerfile
# Build context: repository root (docker build -f src/KnowledgeOps.Worker/Dockerfile .)
#
# Stage 1: Build - restore and publish using the full .NET SDK
# Stage 2: Runtime - run the published output on the .NET runtime image
#          (dotnet/runtime, not aspnet - the Worker does not serve HTTP)
#
# No secrets, connection strings, API keys, or credentials are baked into this image.
# Runtime configuration must be supplied via environment variables or secrets managers.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy source projects only; tests are not needed for publishing the Worker.
COPY Directory.Build.props .
COPY src/ src/

RUN dotnet restore src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj

RUN dotnet publish src/KnowledgeOps.Worker/KnowledgeOps.Worker.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "KnowledgeOps.Worker.dll"]
