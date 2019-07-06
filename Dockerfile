# STAGE01 - Build application and its dependencies
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore

# STAGE02 - Publish the application
FROM build AS publish
WORKDIR /app/Net.Bluewalk.NukiBridge2Mqtt.Console
RUN dotnet publish -c Release -o ../out
RUN rm ../out/*.pdb

# STAGE03 - Create the final image
FROM mcr.microsoft.com/dotnet/core/runtime:2.2-alpine3.9 AS runtime
WORKDIR /app
COPY --from=publish /app/out ./

ENTRYPOINT ["dotnet", "Net.Bluewalk.NukiBridge2Mqtt.Console.dll", "docker"]