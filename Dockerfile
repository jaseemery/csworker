# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY src/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY src/. ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# Install necessary packages for debugging (optional)
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published output from build stage
COPY --from=build /app/out .

# Create non-root user for security
RUN useradd -m -u 1001 workeruser && chown -R workeruser:workeruser /app
USER workeruser

# Health check (optional)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD pgrep -f TemporalWorker || exit 1

# Set environment variables (can be overridden)
ENV DOTNET_ENVIRONMENT=Production
ENV Temporal__Endpoint=temporal:7233
ENV Temporal__Namespace=default
ENV Temporal__TaskQueue=sample-task-queue

# Run the worker
ENTRYPOINT ["dotnet", "CSWorker.dll"]