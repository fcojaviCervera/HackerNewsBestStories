FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero el restore para aprovechar la caché de capas de Docker
COPY src/HackerNewsBestStories.Api/HackerNewsBestStories.Api.csproj src/HackerNewsBestStories.Api/
RUN dotnet restore src/HackerNewsBestStories.Api/HackerNewsBestStories.Api.csproj

COPY src/ src/
RUN dotnet publish src/HackerNewsBestStories.Api/HackerNewsBestStories.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "HackerNewsBestStories.Api.dll"]
