# Build stage - compiles and builds the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["*.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Development stage - used for local development with hot reload
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true

# Install Entity Framework Core tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Delete Migrations folder by creating an entrypoint script
# Copy entrypoint script and make it executable and then set it
COPY entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]

# Production stage - Starts the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "backend.dll"] 
