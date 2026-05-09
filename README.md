# URL Shortener

A production-grade distributed URL shortening service built with .NET, featuring database sharding, load balancing, and async analytics.

## Architecture

- **Load Balancing**: Nginx distributing across 2 API instances
- **Database Sharding**: 4 PostgreSQL shards with deterministic routing
- **Caching**: Redis for hot URLs
- **Analytics**: RabbitMQ + background consumer for click tracking
- **ID Generation**: Snowflake algorithm with Base62 encoding

## Tech Stack

.NET 10 • PostgreSQL • Redis • RabbitMQ • Nginx • Docker

## Quick Start

```bash
docker compose up -d --build
```

**Services:**
- API: `http://localhost`
- RabbitMQ UI: `http://localhost:15672` (user/124578)
- pgAdmin: `http://localhost:5050` (admin@admin.com/admin)

## API Endpoints

```http
POST /url/shorten          # Create short URL
GET /{shortCode}           # Redirect to original
GET /url/{shortCode}/stats # View analytics
GET /health                # Health check
```

## Example

```bash
# Create
curl -X POST http://localhost/url/shorten \
  -H "Content-Type: application/json" \
  -d '{"originalUrl":"https://github.com"}'

# Response: {"shortCode":"xK9pQ2z","shortUrl":"http://localhost/xK9pQ2z",...}

# Use
curl http://localhost/xK9pQ2z  # Redirects to GitHub
```

## Design Highlights

- **Capacity**: 1,160 writes/s, 11,600 reads/s, 365B URLs over 10 years
- **Sharding**: 16 virtual shards → 4 physical (easy resharding)
- **Resilience**: Retry logic, health checks, graceful degradation
- **Performance**: Write-through cache, async analytics processing

## License

MIT