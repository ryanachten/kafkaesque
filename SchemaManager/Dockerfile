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

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS final
WORKDIR /app

# Install the tool globally in the final image
# Global tools are installed to ~/.dotnet/tools, ensure it's in PATH
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global Apache.Avro.Tools

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SchemaManager.dll"]
