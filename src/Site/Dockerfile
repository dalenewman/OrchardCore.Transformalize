#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# FOR AWS APP RUNNER
# ENV ASPNETCORE_URLS http://+:8080
# EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["src/Site/Site.csproj", "src/Site/"]
COPY ["src/OrchardCore.Transformalize/OrchardCore.Transformalize.csproj", "src/OrchardCore.Transformalize/"]
RUN dotnet restore "src/Site/Site.csproj"
COPY . .
WORKDIR "/src/src/Site"
RUN rm -rf ./App_Data/Sites && rm ./App_Data/tenants.json && rm -rf ./App_Data/logs
RUN dotnet build "Site.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Site.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Site.dll"]