# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY KVYS.sln .
COPY src/KVYS.Shared/KVYS.Shared.csproj src/KVYS.Shared/
COPY src/KVYS.Identity/KVYS.Identity.csproj src/KVYS.Identity/
COPY src/KVYS.Api/KVYS.Api.csproj src/KVYS.Api/
COPY src/KVYS.Web/KVYS.Web.csproj src/KVYS.Web/
COPY src/Modules/KVYS.Education/KVYS.Education.csproj src/Modules/KVYS.Education/
COPY src/Modules/KVYS.QualityIndicators/KVYS.QualityIndicators.csproj src/Modules/KVYS.QualityIndicators/
COPY src/Modules/KVYS.Stakeholders/KVYS.Stakeholders.csproj src/Modules/KVYS.Stakeholders/
COPY src/Modules/KVYS.Reporting/KVYS.Reporting.csproj src/Modules/KVYS.Reporting/
COPY src/Modules/KVYS.Archive/KVYS.Archive.csproj src/Modules/KVYS.Archive/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish src/KVYS.Api/KVYS.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Copy published files
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "KVYS.Api.dll"]
