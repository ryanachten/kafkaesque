FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["SchemaManager/SchemaManager.csproj", "SchemaManager/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "SchemaManager/SchemaManager.csproj"

# Copy source code
COPY ["SchemaManager/", "SchemaManager/"]
COPY ["Common/", "Common/"]

# Build
WORKDIR "/src/SchemaManager"
RUN dotnet build "SchemaManager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SchemaManager.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SchemaManager.dll"]
