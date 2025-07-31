# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app .

# OPTIONAL: just to be safe, copy appsettings if not already in /app
# COPY appsettings.json ./appsettings.json
# COPY appsettings.Development.json ./appsettings.Development.json

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Matchboxd.API.dll"]
