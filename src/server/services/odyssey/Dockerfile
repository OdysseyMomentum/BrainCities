FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src

#COPY ["Odyssey.API.csproj", "Odyssey.API/"]
COPY *.csproj ./
RUN dotnet restore --source https://api.nuget.org/v3/index.json

COPY . ./
RUN dotnet publish --no-restore -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Odyssey.API.dll"]
