FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["WsPackets.Test.Server/WsPackets.Test.Server.csproj", "WsPackets.Test.Server/"]
RUN dotnet restore "WsPackets.Test.Server/WsPackets.Test.Server.csproj"
COPY . .
WORKDIR "/src/WsPackets.Test.Server"
RUN dotnet build "WsPackets.Test.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WsPackets.Test.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WsPackets.Test.Server.dll"]
