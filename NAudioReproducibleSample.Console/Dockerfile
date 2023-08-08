FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["NAudioReproducibleSample.Console/NAudioReproducibleSample.Console.csproj", "NAudioReproducibleSample.Console/"]
RUN dotnet restore "NAudioReproducibleSample.Console/NAudioReproducibleSample.Console.csproj"
COPY . .
WORKDIR "/src/NAudioReproducibleSample.Console"
RUN dotnet build "NAudioReproducibleSample.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NAudioReproducibleSample.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NAudioReproducibleSample.Console.dll"]
