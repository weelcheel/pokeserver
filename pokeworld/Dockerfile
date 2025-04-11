FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pokeworld/pokeworld.csproj", "pokeworld/"]
COPY ["pokeframework/pokeframework.csproj", "pokeframework/"]
RUN dotnet restore "pokeworld/pokeworld.csproj"
COPY . .
WORKDIR "/src/pokeworld"
RUN dotnet build "pokeworld.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "pokeworld.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pokeworld.dll"]
