FROM node:22-bookworm-slim AS node
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY --from=node /usr/local/ /usr/local/
WORKDIR /source
COPY . .
RUN dotnet run -- Bundle

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /source/deploy/ ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Server.dll"]
