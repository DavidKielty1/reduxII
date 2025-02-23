# Credit Card Recommendation API

[![.NET Build and Test](https://github.com/DavidKielty1/reduxII/actions/workflows/dotnet.yml/badge.svg)](https://github.com/DavidKielty1/reduxII/actions/workflows/dotnet.yml)

API for retrieving and caching credit card recommendations from multiple providers.

## Design

#### Application Overview

This API provides credit card recommendations by aggregating data from multiple providers, normalizing scores, and caching results for performance optimization.

#### Service Breakdown

- CreditCardController
  Handles HTTP requests for credit card recommendations.
  Validates input, calls the service layer, and returns structured responses.

- CreditCardService
  Manages caching and orchestrates recommendation retrieval.
  Fetches fresh data if cache is unavailable and processes scoring logic.

- CardProviderService
  Fetches data from multiple credit card providers in parallel.
  Normalizes different provider response formats into a common structure.

- ApiService
  Handles HTTP communication with external credit card provider APIs.
  Ensures error handling, request serialization, and response deserialization.

- CardScoreCalculator
  Normalizes credit card eligibility scores from different providers.
  Calculates a weighted sorting score based on eligibility and APR.
  .
- RedisService  
  Implements caching for faster response times and reduced API calls.
  Stores and retrieves credit card recommendations using Redis.

This structure ensures scalability, maintainability, and efficiency while delivering optimized credit card recommendations.

## Features

- Multiple provider integration (CSCards, ScoredCards)
- Redis caching with 10-minute expiration
- Normalized scoring system
- Validation and error handling
- Test coverage requirements

## Deployment

This microservice can be Dockerized with a multi-stage Dockerfile for optimized image size. It can be deployed in Kubernetes using a Deployment with multiple replicas, an HPA (Horizontal Pod Autoscaler) for scaling, and a Service for stable networking. Configurations like API endpoints and Redis credentials are managed via ConfigMaps and Secrets. Redis can be deployed as a pod or a managed service, ensuring scalability, fault tolerance, and efficient load balancing.

## Testing

## Code Coverage

- Line Coverage: ≥80%
- Branch Coverage: ≥80%
- Method Coverage: ≥80%

#### Test Specific Project/Library:

dotnet test Tests/API.Tests/API.Tests.csproj

#### Test Specific Test Class:

dotnet test --filter "FullyQualifiedName~ApiServiceTests"

#### Test Specific Test Method:

dotnet test --filter "FullyQualifiedName=API.Tests.Services.ApiServiceTests.GetCSCards_ReturnsDeserializedResponse"

### Tests coverage:

#### Run tests with coverage:

rm -rf ./Tests/API.Tests/TestResults/\*

dotnet test --collect:"XPlat Code Coverage"

reportgenerator -reports:"./Tests/API.Tests/TestResults/\*/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

#### Open Coverage Report:

open coveragereport/index.html

## CI/CD

This project uses GitHub Actions for continuous integration:

- Automated builds
- Test execution
- Code coverage reporting
- Coverage thresholds enforcement

## Configuration

The application requires the following environment variables:

- `CSCARDS_ENDPOINT`: CSCards API endpoint
- `SCOREDCARDS_ENDPOINT`: ScoredCards API endpoint
- `Redis:ConnectionString`: Redis connection string
