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

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/api ./
COPY --from=build /app/web/wwwroot ./wwwroot

ENTRYPOINT ["dotnet", "CPElite.Api.dll"]
