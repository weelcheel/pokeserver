FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pokeserver/pokeserver.csproj", "pokeserver/"]
COPY ["pokeframework/pokeframework.csproj", "pokeframework/"]
RUN dotnet restore "pokeserver/pokeserver.csproj"
COPY . .
WORKDIR "/src/pokeserver"
RUN dotnet build "pokeserver.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "pokeserver.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pokeserver.dll"]
