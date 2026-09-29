FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore backend/src/WestCoastFitness.Api/WestCoastFitness.Api.csproj --locked-mode \
    && dotnet publish backend/src/WestCoastFitness.Api/WestCoastFitness.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "WestCoastFitness.Api.dll"]
