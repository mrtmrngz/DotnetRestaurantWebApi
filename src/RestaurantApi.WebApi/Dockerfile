# ==========================================
# 1. RESTORE
# ==========================================

ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0 
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra

FROM ${SDK_IMAGE} AS restore

WORKDIR /src

COPY ["src/RestaurantApi.Domain/RestaurantApi.Domain.csproj", "src/RestaurantApi.Domain/"]
COPY ["src/RestaurantApi.Application/RestaurantApi.Application.csproj", "src/RestaurantApi.Application/"]
COPY ["src/RestaurantApi.Infrastructure/RestaurantApi.Infrastructure.csproj", "src/RestaurantApi.Infrastructure/"]
COPY ["src/RestaurantApi.Persistence/RestaurantApi.Persistence.csproj", "src/RestaurantApi.Persistence/"]
COPY ["src/RestaurantApi.WebApi/RestaurantApi.WebApi.csproj", "src/RestaurantApi.WebApi/"]

RUN --mount=type=cache,id=restaurant-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore \
    "src/RestaurantApi.WebApi/RestaurantApi.WebApi.csproj" 

# ==========================================
# 2. DEVELOPMENT
# ==========================================

FROM restore AS development

WORKDIR /src

ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=true

COPY . .

RUN dotnet restore "src/RestaurantApi.WebApi/RestaurantApi.WebApi.csproj"

EXPOSE 8080

CMD ["dotnet", "watch", "run", \
     "--project", "src/RestaurantApi.WebApi/RestaurantApi.WebApi.csproj", \
     "--no-launch-profile"]

# ==========================================
# 3. BUILD
# ==========================================

FROM restore AS build

ARG BUILD_CONFIGURATION=Release

COPY . .

RUN --mount=type=cache,id=restaurant-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet build \
    "src/RestaurantApi.WebApi/RestaurantApi.WebApi.csproj" \
    -c ${BUILD_CONFIGURATION} \
    --no-restore

# ==========================================
# 3. PUBLISH
# ==========================================

FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN --mount=type=cache,id=restaurant-nuget,target=/root/.nuget/packages \
    dotnet publish \
    "src/RestaurantApi.WebApi/RestaurantApi.WebApi.csproj" \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    --no-build \
    -o /app/publish \
    /p:UseAppHost=false 

# ==========================================
# 4. RUNTIME
# ==========================================

FROM ${RUNTIME_IMAGE} AS final

ARG VERSION=dev
ARG REVISION=unknown
ARG CREATED=unknown

LABEL org.opencontainers.image.title="restaurant-api" \
      org.opencontainers.image.description="RestaurantApi WebApi (.NET 10, Onion Architecture)" \
      org.opencontainers.image.source="https://github.com/mrtmrngz/DotnetRestaurantWebApi" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.revision="${REVISION}" \
      org.opencontainers.image.created="${CREATED}"
      
WORKDIR /app

COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
    
EXPOSE 8080
    
USER $APP_UID

ENTRYPOINT ["dotnet", "RestaurantApi.WebApi.dll"]
