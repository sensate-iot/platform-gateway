#
# Docker file for the network API.
#
# @author Michel Megens
# @email  michel@michelmegens.net
#

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

WORKDIR /build

COPY . .
RUN dotnet restore -r linux-x64 SensateIoT.Platform.Network.GatewayAPI/SensateIoT.Platform.Network.GatewayAPI.csproj
RUN dotnet publish -c Release -o /build/binaries -r linux-x64 --no-restore SensateIoT.Platform.Network.GatewayAPI/SensateIoT.Platform.Network.GatewayAPI.csproj

FROM mcr.microsoft.com/dotnet/aspnet:6.0

COPY --from=build-env /build/binaries /app
COPY SensateIoT.Platform.Network.GatewayAPI/appsettings.json /app/appsettings.json

EXPOSE 80
EXPOSE 8080
WORKDIR /app
ENTRYPOINT ["/app/SensateIoT.Platform.Network.GatewayAPI", "--urls", "http://*:80"]
