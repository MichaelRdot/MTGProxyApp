# All of this is from ChatGpt...

# -------- BUILD --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything into the container
COPY . .

# Restore and publish
RUN dotnet restore ./MTGProxyApp.sln
RUN dotnet publish ./MTGProxyApp/MTGProxyApp.csproj -c Release -o /app/publish

# -------- RUN --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Container Apps commonly uses 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MTGProxyApp.dll"]
