# SGC.Novo: the .NET 10 facade + new screens, as a Linux container.
# Build from this folder:  docker build -t sgc-novo .
# Run it next to the legacy site and the database:
#   docker run -p 8080:8080 -e Legado__Url=http://legacy-host/ \
#     -e ReverseProxy__Clusters__legado__Destinations__iis__Address=http://legacy-host/ \
#     -e ConnectionStrings__SGC="Server=...;Database=SGC;..." -e Legado__PonteChave=... sgc-novo

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore together with the sources: restoring from the .csproj alone (without the .razor files) produced
# an image without _framework/blazor.web.js, and the request fell through to the legacy site.
COPY src/Sgc.Novo/ src/Sgc.Novo/
RUN dotnet publish src/Sgc.Novo/Sgc.Novo.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# plain "Urls" overrides the development value in appsettings.json
ENV Urls=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Sgc.Novo.dll"]
