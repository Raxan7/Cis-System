FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Cis.ManagementSystem.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Cis.Api/Cis.Api.csproj", "src/Cis.Api/"]
COPY ["src/Cis.Application/Cis.Application.csproj", "src/Cis.Application/"]
COPY ["src/Cis.Contracts/Cis.Contracts.csproj", "src/Cis.Contracts/"]
COPY ["src/Cis.Domain/Cis.Domain.csproj", "src/Cis.Domain/"]
COPY ["src/Cis.Infrastructure/Cis.Infrastructure.csproj", "src/Cis.Infrastructure/"]
COPY ["tests/Cis.Tests.Unit/Cis.Tests.Unit.csproj", "tests/Cis.Tests.Unit/"]
COPY ["tests/Cis.Tests.Integration/Cis.Tests.Integration.csproj", "tests/Cis.Tests.Integration/"]

RUN dotnet restore "Cis.ManagementSystem.sln"
COPY . .
RUN dotnet publish "src/Cis.Api/Cis.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cis.Api.dll"]
