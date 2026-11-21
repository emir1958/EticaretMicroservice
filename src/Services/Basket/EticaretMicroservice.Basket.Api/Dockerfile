FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Basket/EticaretMicroservice.Basket.Api/EticaretMicroservice.Basket.Api.csproj", "src/Services/Basket/EticaretMicroservice.Basket.Api/"]
COPY ["src/Shared/EticaretMicroservice.Shared/EticaretMicroservice.Shared.csproj", "src/Shared/EticaretMicroservice.Shared/"]

RUN dotnet restore "src/Services/Basket/EticaretMicroservice.Basket.Api/EticaretMicroservice.Basket.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/Basket/EticaretMicroservice.Basket.Api"
RUN dotnet build "EticaretMicroservice.Basket.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EticaretMicroservice.Basket.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Root olmayan kullanıcı (.NET 8 imajındaki "app", UID 1654); 8080 ayrıcalıksız port olduğu için başka ayar gerekmez
USER $APP_UID
ENTRYPOINT ["dotnet", "EticaretMicroservice.Basket.Api.dll"]