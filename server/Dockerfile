FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/GeneaPam.Api/GeneaPam.Api.csproj", "src/GeneaPam.Api/"]
RUN dotnet restore "src/GeneaPam.Api/GeneaPam.Api.csproj"
COPY . .
RUN dotnet publish "src/GeneaPam.Api/GeneaPam.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GeneaPam.Api.dll"]
