#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
RUN apt-get update
RUN apt-get -y install ffmpeg
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["BlobStorage.csproj", "."]
RUN dotnet restore "./BlobStorage.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "BlobStorage.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlobStorage.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlobStorage.dll"]