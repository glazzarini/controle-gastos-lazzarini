# syntax=docker/dockerfile:1
# Build: docker build -t controle-gastos-backend:local backend/

# ── Build (SDK) ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ControleGastos.sln ./
COPY src/ControleGastos.Core/ControleGastos.Core.csproj src/ControleGastos.Core/
COPY src/ControleGastos.Api/ControleGastos.Api.csproj src/ControleGastos.Api/
RUN dotnet restore src/ControleGastos.Api/ControleGastos.Api.csproj
COPY src/ src/
RUN dotnet publish src/ControleGastos.Api/ControleGastos.Api.csproj \
      -c Release -o /app --no-restore -p:UseAppHost=false

# ── Runtime (ASP.NET, Alpine) ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
EXPOSE 8080
# Usuário não-root já existente na imagem oficial (UID 1654)
USER $APP_UID
ENTRYPOINT ["dotnet", "ControleGastos.Api.dll"]
