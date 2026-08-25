# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first to leverage layer caching for restore
COPY ["BudgetTrackerApi/BudgetTrackerApi.csproj", "BudgetTrackerApi/"]
RUN dotnet restore "BudgetTrackerApi/BudgetTrackerApi.csproj"

# Copy remaining source code and publish release build
COPY . .
WORKDIR "/src/BudgetTrackerApi"
RUN dotnet publish "BudgetTrackerApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install Kerberos library to suppress Npgsql lookup warning
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BudgetTrackerApi.dll"]
