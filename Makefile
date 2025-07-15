.PHONY: help build run test docker-build docker-run dev-up dev-down prod-up prod-down logs clean

help:
	@echo "Available commands:"
	@echo "  make build        - Build the .NET project"
	@echo "  make run          - Run the worker locally"
	@echo "  make test         - Run tests"
	@echo "  make docker-build - Build Docker image"
	@echo "  make docker-run   - Run Docker container"
	@echo ""
	@echo "  Development:"
	@echo "  make dev-up       - Start development environment (Temporal + UI + Worker)"
	@echo "  make dev-down     - Stop development environment"
	@echo "  make dev-logs     - View development logs"
	@echo ""
	@echo "  Production:"
	@echo "  make prod-up      - Start production worker only"
	@echo "  make prod-down    - Stop production worker"
	@echo "  make prod-logs    - View production worker logs"
	@echo ""
	@echo "  Utilities:"
	@echo "  make logs         - View all logs"
	@echo "  make clean        - Clean build artifacts and containers"

build:
	cd src && dotnet build

run:
	cd src && dotnet run

test:
	cd tests && dotnet test

docker-build:
	docker build -t temporal-dotnet-worker .

docker-run:
	docker run --rm -it \
		-e Temporal__Endpoint=host.docker.internal:7233 \
		temporal-dotnet-worker

# Development commands
dev-up:
	docker compose -f docker-compose.dev.yml up -d

dev-down:
	docker compose -f docker-compose.dev.yml down

dev-logs:
	docker compose -f docker-compose.dev.yml logs -f

# Production commands
prod-up:
	@echo "Starting production worker..."
	@echo "Make sure to set environment variables:"
	@echo "  TEMPORAL_ENDPOINT=your-temporal-server:7233"
	@echo "  TEMPORAL_NAMESPACE=your-namespace"
	@echo "  TEMPORAL_TASK_QUEUE=your-task-queue"
	@echo ""
	docker compose -f docker-compose.prod.yml up -d

prod-down:
	docker compose -f docker-compose.prod.yml down

prod-logs:
	docker compose -f docker-compose.prod.yml logs -f worker

# Legacy commands (default to dev)
up: dev-up
down: dev-down
logs:
	docker compose logs -f

clean:
	find . -name bin -type d -exec rm -rf {} + 2>/dev/null || true
	find . -name obj -type d -exec rm -rf {} + 2>/dev/null || true
	docker compose -f docker-compose.dev.yml down -v --remove-orphans 2>/dev/null || true
	docker compose -f docker-compose.prod.yml down -v --remove-orphans 2>/dev/null || true
	docker compose down -v --remove-orphans 2>/dev/null || true