FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for restore
COPY ["src/Services/EmulatorService/Emulator.API/Emulator.API.csproj", "src/Services/EmulatorService/Emulator.API/"]
COPY ["src/Services/EmulatorService/Emulator.Service/Emulator.Service.csproj", "src/Services/EmulatorService/Emulator.Service/"]
COPY ["src/Services/EmulatorService/Emulator.Repository/Emulator.Repository.csproj", "src/Services/EmulatorService/Emulator.Repository/"]
COPY ["src/BuildingBlocks/Shared/Shared.csproj", "src/BuildingBlocks/Shared/"]
COPY ["src/BuildingBlocks/Caching/Caching.csproj", "src/BuildingBlocks/Caching/"]
COPY ["src/BuildingBlocks/Contracts/Contracts.csproj", "src/BuildingBlocks/Contracts/"]
COPY ["src/BuildingBlocks/Common.Logging/Common.Logging.csproj", "src/BuildingBlocks/Common.Logging/"]
COPY ["src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj", "src/BuildingBlocks/EventBus.Messages/"]
COPY ["src/BuildingBlocks/Infrastructure/Infrastructure.csproj", "src/BuildingBlocks/Infrastructure/"]
COPY ["src/Services/STEMify-Backend/STEMify-Backend.ServiceDefaults/STEMify-Backend.ServiceDefaults.csproj", "src/Services/STEMify-Backend/STEMify-Backend.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "./src/Services/EmulatorService/Emulator.API/Emulator.API.csproj"

# Copy all source code
COPY . .

# Build the project
WORKDIR "/src/src/Services/EmulatorService/Emulator.API"
RUN dotnet build "./Emulator.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Emulator.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Copy the published app
COPY --from=publish /app/publish .

# Set environment variables for gRPC and SSL/TLS
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# Disable Windows certificate store usage in Linux containers
ENV DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2FLOWCONTROL_DISABLED=1
ENV DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT=1

# Health check for gRPC service
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Emulator.API.dll"]
