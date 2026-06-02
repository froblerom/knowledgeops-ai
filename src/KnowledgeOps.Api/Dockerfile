# KnowledgeOps-AI API - Multi-stage Dockerfile
# Build context: repository root (docker build -f src/KnowledgeOps.Api/Dockerfile .)
#
# Stage 1: Build - restore and publish using the full .NET SDK
# Stage 2: Runtime - run the published output on the ASP.NET Core runtime image (no SDK)
#
# No secrets, connection strings, API keys, or credentials are baked into this image.
# Runtime configuration must be supplied via environment variables or secrets managers.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy source projects only; tests are not needed for publishing the API.
COPY Directory.Build.props .
COPY src/ src/

RUN dotnet restore src/KnowledgeOps.Api/KnowledgeOps.Api.csproj

RUN dotnet publish src/KnowledgeOps.Api/KnowledgeOps.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Listen on port 8080 (non-root, no privileged port required)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "KnowledgeOps.Api.dll"]
