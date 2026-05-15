# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Concoction.slnx ./
COPY Directory.Build.props ./
COPY global.json ./
COPY Concoction.Domain/Concoction.Domain.csproj Concoction.Domain/
COPY Concoction.Application/Concoction.Application.csproj Concoction.Application/
COPY Concoction.Infrastructure/Concoction.Infrastructure.csproj Concoction.Infrastructure/
COPY Concoction.Api/Concoction.Api.csproj Concoction.Api/

RUN dotnet restore Concoction.Api/Concoction.Api.csproj

COPY Concoction.Domain/ Concoction.Domain/
COPY Concoction.Application/ Concoction.Application/
COPY Concoction.Infrastructure/ Concoction.Infrastructure/
COPY Concoction.Api/ Concoction.Api/

RUN dotnet publish Concoction.Api/Concoction.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Concoction.Api.dll"]
