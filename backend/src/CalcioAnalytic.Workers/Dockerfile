# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/CalcioAnalytic.slnx backend/
COPY backend/src/ backend/src/
RUN dotnet restore backend/src/CalcioAnalytic.Workers/CalcioAnalytic.Workers.csproj

RUN dotnet publish backend/src/CalcioAnalytic.Workers/CalcioAnalytic.Workers.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CalcioAnalytic.Workers.dll"]
