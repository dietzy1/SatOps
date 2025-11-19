FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /App

COPY *.sln .
COPY SatOps.csproj ./

RUN dotnet restore SatOps.csproj

COPY . ./


WORKDIR /App
RUN dotnet publish SatOps.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /App
COPY --from=build /App/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SatOps.dll"]