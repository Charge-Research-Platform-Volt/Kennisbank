# Build stage - compiles and builds the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["*.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish backend.csproj -c Release -o /app/publish



# Development stage - used for local development with hot reload
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "watch", "--project", "backend.csproj", "run", "--urls", "http://+:8080"]



# CI Runner scope - used for GitLab CI Pipeline runner
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS ci-runner
WORKDIR /
COPY backend/*.sln /backend/
COPY backend/*.csproj /backend/backend.csproj
COPY backendtests/*.csproj /backendtests/
WORKDIR /backend
RUN dotnet restore
COPY backend/ /backend/
COPY backendtests/ /backendtests/
COPY testdata/ /app/testdata/
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
RUN rm -rf Migrations
CMD ["dotnet", "run", "--project", "backend.csproj", "--urls", "http://+:8080"]


# Production stage - Builds and runs the application for production
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production
WORKDIR /app
RUN apt-get update && apt-get install -y wget curl && rm -rf /var/lib/apt/lists/*
RUN rm -rf Migrations
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "backend.dll", "--urls", "http://+:8080"]

# This program has been developed by students from the bachelor Computer Science at Utrecht
# University within the Software Project course.
# © Copyright Utrecht University (Department of Information and Computing Sciences)
