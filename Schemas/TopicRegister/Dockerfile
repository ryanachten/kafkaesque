FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Schemas/TopicRegister/TopicRegister.csproj", "Schemas/TopicRegister/"]
RUN dotnet restore "Schemas/TopicRegister/TopicRegister.csproj"

COPY ["Schemas/TopicRegister/", "Schemas/TopicRegister/"]
COPY ["Schemas/kafka-topics.yml", "Schemas/"]

WORKDIR "/src/Schemas/TopicRegister"
RUN dotnet build "TopicRegister.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TopicRegister.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=build /src/Schemas/kafka-topics.yml .

ENTRYPOINT ["dotnet", "TopicRegister.dll"]
