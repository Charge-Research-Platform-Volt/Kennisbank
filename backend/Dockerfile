FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
ENV HOST_URL=http://localhost:3000
#   DATABASE_CONNECTION_STRING required
ENV EMAIL_SMTP_HOST=smtp-relay.gmail.com
RUN echo EMAIL_SMTP_HOST is ${EMAIL_SMTP_HOST}
RUN echo $EMAIL_SMTP_HOST
#   EMAIL_SMTP_HOST required
ENV EMAIL_TLS_PORT=587
#   EMAIL_ADDRESS required
#   EMAIL_PASSWORD required
#   EMAIL_FROM_NAME required
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
RUN apt-get update && apt-get install -y curl chromium && rm -rf /var/lib/apt/lists/*
EXPOSE $PORT
# Use CMD so we can override it in docker-compose in case we want to run watch mode
CMD exec dotnet run --project backend.csproj --urls http://+:${PORT}

# CI Runner scope - used for GitLab CI Pipeline runner
FROM base AS ci-runner
ENV ASPNETCORE_ENVIRONMENT=Production
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
EXPOSE $PORT
CMD exec dotnet run --project backend.csproj --urls http://+:${PORT}


# Production stage - Builds and runs the application for production
FROM base AS production
ENV ASPNETCORE_ENVIRONMENT=Production
WORKDIR /app
RUN apt-get update && apt-get install -y wget curl chromium && rm -rf /var/lib/apt/lists/*
RUN rm -rf Migrations
COPY --from=build /app/publish .
EXPOSE $PORT
ENTRYPOINT exec dotnet backend.dll --urls http://+:${PORT}

# This program has been developed by students from the bachelor Computer Science at Utrecht
# University within the Software Project course.
# © Copyright Utrecht University (Department of Information and Computing Sciences)
