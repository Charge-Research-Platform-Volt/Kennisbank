FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
ENV PORT=8080

# Build stage - compiles and builds the application
FROM base AS build
WORKDIR /src
COPY ["*.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish backend.csproj -c Release -o /app/publish

# Development stage - used for local development with hot reload
FROM base AS development
ENV ASPNETCORE_ENVIRONMENT=Development
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE $PORT
# Use CMD so we can override it in docker-compose in case we want to run watch mode
CMD exec dotnet run --project backend.csproj --urls http://+:${PORT}

# Production stage - runtime-only image (no SDK/build tools), runs as non-root user
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
WORKDIR /app
RUN apt-get update && apt-get install -y wget curl && rm -rf /var/lib/apt/lists/*
COPY --from=build --chown=app:app /app/publish .
RUN mkdir -p /app/logs && chown app:app /app /app/logs
USER app
EXPOSE $PORT
ENTRYPOINT exec dotnet backend.dll --urls http://+:${PORT}
