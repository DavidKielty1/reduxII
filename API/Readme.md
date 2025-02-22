## Testing

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
