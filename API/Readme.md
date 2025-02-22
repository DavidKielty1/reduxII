


## Testing

#### Test Specific Project/Library:

dotnet test Tests/API.Tests/API.Tests.csproj

#### Test Specific Test Class:

dotnet test --filter "FullyQualifiedName~CreditCardControllerTests"

#### Test Specific Test Method:

dotnet test --filter "FullyQualifiedName=API.Tests.Controllers.CreditCardControllerTests.ProcessCreditCard_ValidatesRequest"

### Tests coverage:

#### Run tests with coverage:

dotnet test --collect:"XPlat Code Coverage"

#### Generate Coverage Report (after running tests with coverage):

reportgenerator -reports:"./Tests/API.Tests/TestResults/\*/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

#### Open Coverage Report:

open coveragereport/index.html
