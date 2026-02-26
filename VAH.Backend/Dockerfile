# ============================================================
# VAH Backend — Multi-stage Dockerfile
# Build: dotnet SDK → Publish: ASP.NET runtime
# ============================================================

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore first (for caching)
COPY VAH.Backend.csproj ./
RUN dotnet restore

# Copy all source and publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Create required directories
RUN mkdir -p /app/wwwroot/uploads /app/wwwroot/uploads/thumbs /app/logs \
    && chown -R appuser:appgroup /app

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser

# Expose port
EXPOSE 5027

# Environment variables
ENV ASPNETCORE_URLS=http://+:5027
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "VAH.Backend.dll"]
