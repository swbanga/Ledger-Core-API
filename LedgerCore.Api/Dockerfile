# STAGE 1: The Builder (Contains full .NET 10 SDK and Roslyn compiler)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to cache the NuGet restore layer
COPY ["LedgerCore.Api/LedgerCore.Api.csproj", "LedgerCore.Api/"]
COPY ["LedgerCore.Application/LedgerCore.Application.csproj", "LedgerCore.Application/"]
COPY ["LedgerCore.Domain/LedgerCore.Domain.csproj", "LedgerCore.Domain/"]
COPY ["LedgerCore.Infrastructure/LedgerCore.Infrastructure.csproj", "LedgerCore.Infrastructure/"]

RUN dotnet restore "LedgerCore.Api/LedgerCore.Api.csproj"

# Copy the rest of the source code and compile
COPY . .
WORKDIR "/src/LedgerCore.Api"
RUN dotnet build "LedgerCore.Api.csproj" -c Release -o /app/build
RUN dotnet publish "LedgerCore.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 2: The Runtime (Stripped down, highly secure, no compiler tools)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy only the compiled binaries from Stage 1
COPY --from=build /app/publish .

# Define the entrypoint physics
ENTRYPOINT ["dotnet", "LedgerCore.Api.dll"]