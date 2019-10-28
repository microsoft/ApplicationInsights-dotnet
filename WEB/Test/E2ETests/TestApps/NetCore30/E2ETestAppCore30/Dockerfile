#FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build-env
#WORKDIR /app

# copy csproj and restore as distinct layers
#COPY *.csproj ./
#RUN dotnet restore

# copy everything else and build
#COPY . ./
#RUN dotnet publish -c Release -r win-x64 -o out

#FROM mcr.microsoft.com/dotnet/core/aspnet:3.0
#WORKDIR /app

#COPY --from=build-env /app/out ./
#ENTRYPOINT ["./E2ETestAppCore30"]

FROM mcr.microsoft.com/dotnet/core/sdk:3.0
WORKDIR /app
EXPOSE 80
COPY . /app
#RUN ["dir" "/app"]
ENTRYPOINT ["dotnet", "E2ETestAppCore30.dll"]
