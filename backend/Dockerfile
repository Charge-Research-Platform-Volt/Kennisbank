FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["*.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /src
COPY ["backendtests/backendtests.csproj", "backendtests/"]
COPY . .
RUN dotnet restore "backendtests/backendtests.csproj"
RUN dotnet test "backendtests/backendtests.csproj" --logger "trx"
WORKDIR /app
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
ENTRYPOINT ["dotnet", "watch", "--project", "backend.csproj", "run", "--urls", "http://+:8080"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "backend.dll"]   