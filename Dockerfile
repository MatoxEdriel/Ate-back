# =============================================================================
# Build Stage (.NET 10 SDK)
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy project files first for optimized Docker layer caching
COPY ["Ate.Domain/Ate.Domain.csproj", "Ate.Domain/"]
COPY ["Ate.Application/Ate.Application.csproj", "Ate.Application/"]
COPY ["Ate.Infrastructure/Ate.Infrastructure.csproj", "Ate.Infrastructure/"]
COPY ["Ate.Api/Ate.Api.csproj", "Ate.Api/"]

# Restore dependencies
RUN dotnet restore "Ate.Api/Ate.Api.csproj"

# Copy full source code
COPY . .

# Publish release package
RUN dotnet publish "Ate.Api/Ate.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =============================================================================
# Runtime Stage (ASP.NET Core 10 Runtime - Alpine Linux)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Expose standard container port
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published artifacts
COPY --from=build /app/publish .

# Run as built-in non-root user (app) for container security
USER app

ENTRYPOINT ["dotnet", "Ate.Api.dll"]
