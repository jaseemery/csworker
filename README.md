# NIWorker

A production-ready template for building Temporal workers using .NET 9 and Docker.

## Features

- .NET 9 based Temporal worker
- Modern dependency injection using Temporalio.Extensions.Hosting
- Sample workflows and activities with best practices
- Clean registration pattern - easily add workflows and activities
- Docker support with multi-stage builds
- Separate development and production configurations
- Proper configuration management
- Structured logging with dependency injection
- Health checks
- Retry policies and timeouts
- Activity heartbeating
- Graceful shutdown handling

## Project Structure

```
NIWorker/
├── src/
│   ├── Program.cs                      # Worker entry point
│   ├── NIWorker.csproj                 # Project file
│   ├── appsettings.json                # Configuration
│   ├── appsettings.Development.json
│   ├── Workflows/
│   │   └── SampleWorkflow.cs           # Sample workflow implementation
│   └── Activities/
│       └── SampleActivities.cs         # Sample activities
├── tests/
│   ├── NIWorker.Tests.csproj           # Test project
│   └── SampleWorkflowTests.cs          # Unit tests
├── Dockerfile                          # Multi-stage Docker build
├── docker-compose.yml                  # Default (development) setup
├── docker-compose.dev.yml              # Full development stack
├── docker-compose.prod.yml             # Production worker only
├── .env.example                        # Environment variables template
├── Makefile                            # Build and deployment commands
└── README.md
```

## Quick Start

### Prerequisites

- Docker and Docker Compose
- .NET 9 SDK (for local development)

### Development Mode (Full Stack)

Start complete development environment with Temporal server, UI, and worker:

```bash
# Using make commands
make dev-up
make dev-logs
make dev-down

# Using docker compose directly
docker compose -f docker-compose.dev.yml up -d
docker compose -f docker-compose.dev.yml logs -f
docker compose -f docker-compose.dev.yml down
```

Services started:
- Temporal server (localhost:7233)
- Temporal UI (http://localhost:8080)
- PostgreSQL database (localhost:5432)
- .NET worker with debug logging

### Production Mode (Worker Only)

Deploy worker to connect to external Temporal server:

```bash
# Configure environment
cp .env.example .env
# Edit .env with your production settings

# Using make commands
make prod-up
make prod-logs
make prod-down

# Using docker compose directly
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml logs -f worker
docker compose -f docker-compose.prod.yml down
```

### Local Development

Run worker locally while using containerized Temporal:

```bash
# Start infrastructure only
make dev-up

# Run worker locally
make run
# or
cd src && dotnet run
```

## Configuration

### Development Configuration

Configure via `appsettings.json` and `appsettings.Development.json`:

```json
{
  "Temporal": {
    "Endpoint": "localhost:7233",
    "Namespace": "default",
    "TaskQueue": "sample-task-queue"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Temporalio": "Debug"
    }
  }
}
```

### Production Configuration

Configure via environment variables in `.env` file:

```bash
TEMPORAL_ENDPOINT=your-temporal-server:7233
TEMPORAL_NAMESPACE=your-namespace
TEMPORAL_TASK_QUEUE=your-task-queue
LOG_LEVEL=Information
TEMPORAL_LOG_LEVEL=Information
WORKER_LOG_LEVEL=Information
```

Environment variable format follows: `Temporal__Endpoint`, `Temporal__Namespace`, etc.

## Testing the Sample Workflows

Test the included sample workflows using Temporal CLI:

### SampleWorkflow
```bash
temporal workflow execute \
  --address localhost:7233 \
  --namespace default \
  --type SampleWorkflow \
  --task-queue sample-task-queue \
  --input '{"Name": "John", "Data": ["item1", "item2", "item3"], "ProcessingType": "batch"}'
```

### NotificationWorkflow
```bash
temporal workflow execute \
  --address localhost:7233 \
  --namespace default \
  --type NotificationWorkflow \
  --task-queue sample-task-queue \
  --input '{"UserId": "user123", "Email": "test@example.com", "PhoneNumber": "+1234567890", "Subject": "Welcome", "Message": "Welcome to our service"}'
```

The sample workflows demonstrate:
- Workflow implementation with input parameters
- Activity execution with retry policies
- Proper timeout configuration
- Activity heartbeating for long-running tasks
- Dependency injection in activities
- Structured logging
- Multiple activity coordination

## Running Tests

```bash
# Run unit tests
make test

# Or directly
cd tests && dotnet test
```

## Build Commands

Available make commands:

```bash
make help          # Show all available commands
make build         # Build the .NET project
make test          # Run tests
make dev-up        # Start development environment
make dev-down      # Stop development environment
make dev-logs      # View development logs
make prod-up       # Start production worker
make prod-down     # Stop production worker
make prod-logs     # View production worker logs
make clean         # Clean build artifacts and containers
```

## Deployment Examples

### Docker Swarm

```bash
docker stack deploy -c docker-compose.prod.yml niworker-stack
```

### Kubernetes

```bash
# Build and push image
docker build -t your-registry/niworker:latest .
docker push your-registry/niworker:latest

# Deploy (requires kubernetes manifests)
kubectl apply -f k8s/
```

### Production Environment

```bash
# Set production environment variables
export TEMPORAL_ENDPOINT=temporal.yourcompany.com:7233
export TEMPORAL_NAMESPACE=production
export TEMPORAL_TASK_QUEUE=production-workers

# Start production worker
make prod-up
```

## Extending the Template

### Adding New Workflows

1. Create workflow class in `src/Workflows/`
2. Add `[Workflow]` attribute to class
3. Implement `[WorkflowRun]` method
4. Register in `Program.cs`: `.AddWorkflow<YourWorkflow>()`

Example:
```csharp
[Workflow]
public class OrderProcessingWorkflow
{
    [WorkflowRun]
    public async Task<OrderResult> RunAsync(OrderInput input)
    {
        // Workflow logic here
    }
}

// Register in Program.cs
.AddWorkflow<OrderProcessingWorkflow>()
```

### Adding New Activities

1. Create activity class in `src/Activities/`
2. Add `[Activity]` attribute to methods
3. Use dependency injection in constructor
4. Register in `Program.cs`: `.AddScopedActivities<YourActivityClass>()`

Example:
```csharp
public class EmailActivities
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailActivities> _logger;

    public EmailActivities(IEmailService emailService, ILogger<EmailActivities> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [Activity]
    public async Task SendEmailAsync(string email, string subject, string body)
    {
        await _emailService.SendAsync(email, subject, body);
    }
}

// Register services and activities in Program.cs
builder.Services.AddScoped<IEmailService, EmailService>();
// ...
.AddScopedActivities<EmailActivities>()
```

### Registration Options

- **`.AddScopedActivities<T>()`** - New instance per activity execution (recommended)
- **`.AddSingletonActivities<T>()`** - Single instance shared across executions
- **`.AddTransientActivities<T>()`** - New instance each time requested

### Dependency Injection Benefits

- Activities can use standard .NET DI container
- Constructor injection for services like ILogger, HttpClient, database contexts
- Scoped lifetime ensures proper resource management
- Easy testing with mock services
- Follows standard .NET patterns

### Adding Tests

Create test classes in `tests/` folder following xUnit patterns.

## Monitoring and Observability

- Worker outputs structured logs to console
- Temporal UI provides workflow execution history at http://localhost:8080
- Production worker includes health checks
- Resource limits configured for production deployment

## Security Considerations

- Docker image runs as non-root user
- Use environment variables for sensitive configuration
- Consider TLS for production Temporal connections
- Secret management recommended for production deployments

## Troubleshooting

### Worker Connection Issues
- Verify Temporal endpoint configuration
- Check network connectivity (especially in Docker)
- Confirm namespace exists in Temporal cluster

### Activity Timeouts
- Increase activity timeout in workflow configuration
- Ensure activities send heartbeats for long operations
- Check worker resource constraints and scaling

### Build Issues
- Ensure .NET 9 SDK is installed
- Verify Docker daemon is running
- Check file permissions in containerized environments

## License

This template is provided as-is for use in your projects.