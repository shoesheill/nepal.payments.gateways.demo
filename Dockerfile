# Use official .NET 9 SDK and runtime images (multi-arch compatible)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Nepal.Payments.Gateways.Demo/Nepal.Payments.Gateways.Demo.csproj", "Nepal.Payments.Gateways.Demo/"]
RUN dotnet restore "Nepal.Payments.Gateways.Demo/Nepal.Payments.Gateways.Demo.csproj"
COPY . .
WORKDIR "/src/Nepal.Payments.Gateways.Demo"
RUN dotnet build "Nepal.Payments.Gateways.Demo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nepal.Payments.Gateways.Demo.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nepal.Payments.Gateways.Demo.dll"]
