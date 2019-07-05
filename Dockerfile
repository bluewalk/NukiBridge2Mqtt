FROM mcr.microsoft.com/dotnet/core/runtime:2.2-alpine3.9

COPY bin/Release/netcoreapp2.2 /app
COPY config.yml /app
COPY log4net.config /app

WORKDIR /app

EXPOSE 8080

ENTRYPOINT ["dotnet", "Net.Bluewalk.NukiBridge2Mqtt.Console.dll", "docker"]