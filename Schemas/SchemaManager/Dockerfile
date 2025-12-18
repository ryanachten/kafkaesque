FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files maintaining the correct directory structure
COPY ["Schemas/SchemaManager/SchemaManager.csproj", "Schemas/SchemaManager/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "Schemas/SchemaManager/SchemaManager.csproj"

# Copy source code
COPY ["Schemas/SchemaManager/", "Schemas/SchemaManager/"]
COPY ["Common/", "Common/"]
COPY ["Schemas/Avro/", "Schemas/Avro/"]

# Build
WORKDIR "/src/Schemas/SchemaManager"
RUN dotnet build "SchemaManager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SchemaManager.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=build /src/Schemas/Avro ./Avro

ENTRYPOINT ["dotnet", "SchemaManager.dll"]
