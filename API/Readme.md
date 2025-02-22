# Credit Card Recommendation API

[![.NET Build and Test](https://github.com/{your-username}/{repo-name}/actions/workflows/dotnet.yml/badge.svg)](https://github.com/{your-username}/{repo-name}/actions/workflows/dotnet.yml)

API for retrieving and caching credit card recommendations from multiple providers.

## Features

- Multiple provider integration (CSCards, ScoredCards)
- Redis caching with 10-minute expiration
- Normalized scoring system
- Validation and error handling
- Test coverage requirements

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
