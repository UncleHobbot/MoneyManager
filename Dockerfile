# Stage 1: Build React frontend
FROM node:22-alpine AS frontend
WORKDIR /app/frontend
COPY src/moneymanager-web/package*.json ./
RUN npm ci --ignore-scripts
COPY src/moneymanager-web/ .
RUN npm run build

# Stage 2: Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY src/MoneyManager.Api/MoneyManager.Api.csproj ./
RUN dotnet restore
COPY src/MoneyManager.Api/ .
RUN dotnet publish -c Release -o /publish --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0

LABEL org.opencontainers.image.title="MoneyManager" \
      org.opencontainers.image.description="Personal finance management web application" \
      org.opencontainers.image.source="https://github.com/UncleHobbot/MoneyManager" \
      org.opencontainers.image.licenses="MIT"

RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    addgroup --gid 1654 --system appgroup && \
    adduser --uid 1654 --system --ingroup appgroup appuser && \
    mkdir -p /app/data /app/backups /app/csv-archive /app/template && \
    chown -R appuser:appgroup /app

WORKDIR /app

COPY --from=backend --chown=appuser:appgroup /publish .
COPY --from=frontend --chown=appuser:appgroup /app/frontend/dist ./wwwroot
COPY --chown=appuser:appgroup legacy/Data/MoneyManagerEmpty.db /app/template/MoneyManagerEmpty.db
COPY --chown=appuser:appgroup docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

USER appuser

EXPOSE 8080
VOLUME ["/app/data", "/app/backups", "/app/csv-archive"]

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/MoneyManager.db"

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["/app/docker-entrypoint.sh"]
