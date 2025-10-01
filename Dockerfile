FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5001
EXPOSE 6001


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# copy the project file from the same folder as the Dockerfile
COPY ["src/Gateways.csproj", "."]

# restore using the project file in /src
RUN dotnet restore "./Gateways.csproj"

# copy the rest of the source
COPY . .

WORKDIR "/src/."
RUN dotnet build "./src/Gateways.csproj" -c $BUILD_CONFIGURATION -o /app/build
# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./src/Gateways.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateways.dll"]