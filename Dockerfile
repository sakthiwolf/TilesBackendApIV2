# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy and restore the project
COPY ["TilesBackendApI.csproj", "./"]
RUN dotnet restore "TilesBackendApI.csproj"

# Copy the rest of the files and build
COPY . .
RUN dotnet build "TilesBackendApI.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "TilesBackendApI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TilesBackendApI.dll"]
