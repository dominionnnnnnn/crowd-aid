# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (cached layer as long as the project files don't change)
COPY CrowdAid.sln ./
COPY CrowdAid.Api/CrowdAid.Api.csproj CrowdAid.Api/
COPY CrowdAid.Service/CrowdAid.Service.csproj CrowdAid.Service/
COPY CrowdAid.Data/CrowdAid.Data.csproj CrowdAid.Data/
RUN dotnet restore CrowdAid.Api/CrowdAid.Api.csproj

COPY . .
RUN dotnet publish CrowdAid.Api/CrowdAid.Api.csproj -c Release -o /app/publish --no-restore

# ---------- RUN ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8 images listen on 8080 and ship a non-root "app" user
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "CrowdAid.Api.dll"]
