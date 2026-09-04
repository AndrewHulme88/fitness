FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["backend/FitnessCoach.Api/FitnessCoach.Api.csproj", "backend/FitnessCoach.Api/"]
COPY ["backend/FitnessCoach.Api/packages.lock.json", "backend/FitnessCoach.Api/"]

RUN dotnet restore "backend/FitnessCoach.Api/FitnessCoach.Api.csproj" --locked-mode

COPY . .

RUN dotnet publish "backend/FitnessCoach.Api/FitnessCoach.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build --chown=$APP_UID /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "FitnessCoach.Api.dll"]
