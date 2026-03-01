# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/AppLogica.Desk.Domain/AppLogica.Desk.Domain.csproj src/AppLogica.Desk.Domain/
COPY src/AppLogica.Desk.Application/AppLogica.Desk.Application.csproj src/AppLogica.Desk.Application/
COPY src/AppLogica.Desk.Infrastructure/AppLogica.Desk.Infrastructure.csproj src/AppLogica.Desk.Infrastructure/
COPY src/AppLogica.Desk.API/AppLogica.Desk.API.csproj src/AppLogica.Desk.API/

RUN dotnet restore src/AppLogica.Desk.API/AppLogica.Desk.API.csproj

COPY src/ src/
RUN dotnet publish src/AppLogica.Desk.API/AppLogica.Desk.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AppLogica.Desk.API.dll"]
