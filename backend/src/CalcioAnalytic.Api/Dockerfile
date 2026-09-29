# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the solution for deterministic layer caching.
COPY backend/CalcioAnalytic.slnx backend/
COPY backend/src/ backend/src/
RUN dotnet restore backend/src/CalcioAnalytic.Api/CalcioAnalytic.Api.csproj

RUN dotnet publish backend/src/CalcioAnalytic.Api/CalcioAnalytic.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CalcioAnalytic.Api.dll"]
