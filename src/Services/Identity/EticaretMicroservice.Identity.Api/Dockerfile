FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Identity/EticaretMicroservice.Identity.Api/EticaretMicroservice.Identity.Api.csproj", "src/Services/Identity/EticaretMicroservice.Identity.Api/"]
COPY ["src/Shared/EticaretMicroservice.Shared/EticaretMicroservice.Shared.csproj", "src/Shared/EticaretMicroservice.Shared/"]

RUN dotnet restore "src/Services/Identity/EticaretMicroservice.Identity.Api/EticaretMicroservice.Identity.Api.csproj"

COPY . .
WORKDIR "/src/src/Services/Identity/EticaretMicroservice.Identity.Api"
RUN dotnet build "EticaretMicroservice.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EticaretMicroservice.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Root olmayan kullanıcı (.NET 8 imajındaki "app", UID 1654); 8080 ayrıcalıksız port olduğu için başka ayar gerekmez
USER $APP_UID
ENTRYPOINT ["dotnet", "EticaretMicroservice.Identity.Api.dll"]