#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Site/Site.csproj", "src/Site/"]
COPY ["src/OrchardCore.Transformalize/OrchardCore.Transformalize.csproj", "src/OrchardCore.Transformalize/"]
COPY ["nuget.config",""]
RUN dotnet restore "src/Site/Site.csproj"
COPY . .
WORKDIR "/src/src/Site"
RUN dotnet build "Site.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Site.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Site.dll"]