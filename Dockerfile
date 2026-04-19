# Stage 1: Build React
FROM node:22-alpine AS frontend
WORKDIR /app/frontend
COPY src/moneymanager-web/package*.json ./
RUN npm ci
COPY src/moneymanager-web/ .
RUN npm run build

# Stage 2: Build .NET
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /app
COPY src/MoneyManager.Api/*.csproj ./
RUN dotnet restore
COPY src/MoneyManager.Api/ .
RUN dotnet publish -c Release -o /publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /publish .
COPY --from=frontend /app/frontend/dist ./wwwroot

EXPOSE 8080
VOLUME ["/app/data", "/app/backups", "/app/csv-archive"]

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/MoneyManager.db"

ENTRYPOINT ["dotnet", "MoneyManager.Api.dll"]
