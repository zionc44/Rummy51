# The publish Visual Studio runs, in three stages: the client, the server
# with the client in its wwwroot, and a runtime holding only the result.

FROM node:24-bookworm-slim AS client
WORKDIR /src
COPY rummy51.client/package.json rummy51.client/package-lock.json ./
RUN npm ci
COPY rummy51.client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server
WORKDIR /src
COPY Rummy51.Domain/Rummy51.Domain.csproj Rummy51.Domain/
COPY Rummy51.Application/Rummy51.Application.csproj Rummy51.Application/
COPY Rummy51.Infrastructure/Rummy51.Infrastructure.csproj Rummy51.Infrastructure/
COPY Rummy51.Server/Rummy51.Server.csproj Rummy51.Server/
RUN dotnet restore Rummy51.Server/Rummy51.Server.csproj -p:SkipClientBuild=true
COPY Rummy51.Domain/ Rummy51.Domain/
COPY Rummy51.Application/ Rummy51.Application/
COPY Rummy51.Infrastructure/ Rummy51.Infrastructure/
COPY Rummy51.Server/ Rummy51.Server/
# Before the publish and not after it: MapStaticAssets serves what the
# publish found in wwwroot, and a file copied in later answers 404.
COPY --from=client /src/dist/rummy51.client/browser/ Rummy51.Server/wwwroot/
RUN dotnet publish Rummy51.Server/Rummy51.Server.csproj -c Release -o /app --no-restore -p:SkipClientBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=server /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Rummy51.Server.dll"]
