FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Eaat.DeliveryService/Eaat.DeliveryService.csproj", "src/Eaat.DeliveryService/"]
RUN dotnet restore "src/Eaat.DeliveryService/Eaat.DeliveryService.csproj"
COPY . .
WORKDIR "/src/src/Eaat.DeliveryService"
RUN dotnet build "./Eaat.DeliveryService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Eaat.DeliveryService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Eaat.DeliveryService.dll"]
