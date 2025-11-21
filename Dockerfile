# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["CTSAR.Booking.csproj", "./"]
RUN dotnet restore "CTSAR.Booking.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "CTSAR.Booking.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "CTSAR.Booking.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Expose port 8080 (standard for containerized apps)
EXPOSE 8080

# Copy published files from build stage
COPY --from=build /app/publish .

# Run the application
ENTRYPOINT ["dotnet", "CTSAR.Booking.dll"]
