# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CPElite.sln ./
COPY src/CPElite.Api/CPElite.Api.csproj src/CPElite.Api/
COPY src/CPElite.Application/CPElite.Application.csproj src/CPElite.Application/
COPY src/CPElite.Contracts/CPElite.Contracts.csproj src/CPElite.Contracts/
COPY src/CPElite.Domain/CPElite.Domain.csproj src/CPElite.Domain/
COPY src/CPElite.Infrastructure/CPElite.Infrastructure.csproj src/CPElite.Infrastructure/
COPY src/CPElite.Web/CPElite.Web.csproj src/CPElite.Web/
COPY tests/CPElite.Tests.Integration/CPElite.Tests.Integration.csproj tests/CPElite.Tests.Integration/
COPY tests/CPElite.Tests.Unit/CPElite.Tests.Unit.csproj tests/CPElite.Tests.Unit/

RUN dotnet restore CPElite.sln

COPY . .

RUN dotnet publish src/CPElite.Web/CPElite.Web.csproj -c Release -o /app/web
RUN dotnet publish src/CPElite.Api/CPElite.Api.csproj -c Release -o /app/api /p:UseAppHost=false
RUN rm -rf /app/api/wwwroot \
    && mkdir -p /app/api/wwwroot \
    && cp -a /app/web/wwwroot/. /app/api/wwwroot/ \
    && test -f /app/api/wwwroot/index.html \
    && test -f /app/api/wwwroot/_framework/blazor.webassembly.js \
    && test -f /app/api/wwwroot/_framework/blazor.boot.json \
    && test -f /app/api/wwwroot/CPElite.Web.styles.css \
    && find /app/api/wwwroot/_framework -maxdepth 1 -type f -name 'icudt_EFIGS*.dat' | grep -q . \
    && grep -o '"[^"]*\.\(wasm\|dat\|js\)"[[:space:]]*:' /app/api/wwwroot/_framework/blazor.boot.json \
        | cut -d '"' -f 2 \
        | while read asset; do \
            if [ -n "$asset" ] && [ ! -f "/app/api/wwwroot/_framework/$asset" ]; then \
                echo "Missing Blazor framework asset: $asset"; \
                exit 1; \
            fi; \
        done

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV PORT=8080

EXPOSE 8080

COPY --from=build /app/api ./
RUN test -f /app/wwwroot/index.html \
    && test -f /app/wwwroot/_framework/blazor.webassembly.js \
    && test -f /app/wwwroot/_framework/blazor.boot.json \
    && find /app/wwwroot/_framework -maxdepth 1 -type f -name 'icudt_EFIGS*.dat' | grep -q .

ENTRYPOINT ["dotnet", "CPElite.Api.dll"]
