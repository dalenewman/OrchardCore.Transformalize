#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:7.0
ARG BUILD_IMAGE=mcr.microsoft.com/dotnet/sdk:7.0

FROM ${BASE_IMAGE} AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# FOR AWS APP RUNNER
# ENV ASPNETCORE_URLS http://+:8080
# EXPOSE 8080

FROM ${BUILD_IMAGE} AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["src/Site/Site.csproj", "src/Site/"]
COPY ["src/OrchardCore.Transformalize/OrchardCore.Transformalize.csproj", "src/OrchardCore.Transformalize/"]
COPY ["src/OrchardCore.Proxy/OrchardCore.Proxy.csproj", "src/OrchardCore.Proxy/"]
RUN dotnet restore "src/Site/Site.csproj"
COPY . .
WORKDIR "/src/src/Site"
RUN dotnet build "Site.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Site.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Site.dll"]
