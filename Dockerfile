# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["control-lavanderia.csproj", "./"]
RUN dotnet restore "control-lavanderia.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "control-lavanderia.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# ASP.NET Core container defaults
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "control-lavanderia.dll"]
