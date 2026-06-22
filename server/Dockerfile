FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Genea.Api/Genea.Api.csproj", "src/Genea.Api/"]
RUN dotnet restore "src/Genea.Api/Genea.Api.csproj"
COPY . .
RUN dotnet publish "src/Genea.Api/Genea.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Genea.Api.dll"]
