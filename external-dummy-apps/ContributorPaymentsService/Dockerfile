# Use the official .NET SDK chiselled image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY . .

RUN dotnet restore

RUN dotnet publish ./external-dummy-apps/ContributorPaymentsService/ContributorPaymentsService.csproj -c Release -o out

# Use the official .NET runtime chiselled image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Expose the default ports for ASP.NET Core
EXPOSE 8080
EXPOSE 443

# Set the entry point for the container
ENTRYPOINT ["dotnet", "ContributorPaymentsService.dll"]