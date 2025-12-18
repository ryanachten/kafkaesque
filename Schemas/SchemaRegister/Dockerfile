FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Schemas/SchemaRegister/SchemaRegister.csproj", "Schemas/SchemaRegister/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "Schemas/SchemaRegister/SchemaRegister.csproj"

COPY ["Schemas/SchemaRegister/", "Schemas/SchemaRegister/"]
COPY ["Common/", "Common/"]
COPY ["Schemas/Avro/", "Schemas/Avro/"]

WORKDIR "/src/Schemas/SchemaRegister"
RUN dotnet build "SchemaRegister.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SchemaRegister.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=build /src/Schemas/Avro ./Avro

ENTRYPOINT ["dotnet", "SchemaRegister.dll"]
