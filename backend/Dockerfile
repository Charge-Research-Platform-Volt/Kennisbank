FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["*.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish
COPY ["backendtests/backendtests.csproj", "backendtests/"]
RUN dotnet restore "backendtests/backendtests.csproj"
RUN dotnet build "backendtests/backendtests.csproj" -c Release
RUN dotnet test --no-build -c Release --results-directory /testresults --logger "trx;LogFileName=test_result.xml" TestWebApp/TestWebApp.csproj

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
ENTRYPOINT ["dotnet", "watch", "--project", "backend.csproj", "run", "--urls", "http://+:8080"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "backend.dll"]   