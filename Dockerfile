# Stage 1: Build SmsControl dll file (Build stage).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
##AS builder

# Use /bin/bash as the shell, and exit immediately if any command in a list fails.
SHELL ["/bin/bash", "-e", "-c"]

# Set the working directory inside the container
WORKDIR /app

# Install Maven and build the application
#RUN apt-get update && \
#    apt-get install -y maven && \
#    rm -rf /var/lib/apt/lists/*

# Copy the .csproj file and restore any dependencies (via 'dotnet restore')
COPY *.csproj ./
RUN dotnet restore

# Copy the entire source code into the container
COPY . ./

# Publish the app to a folder named 'out'
# RUN dotnet build SmsControl.csproj -c Debug -o out
RUN dotnet publish -c Release -o out

# Stage 2: Run application (Runtime stage)
# Use the official .NET Runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Set the working directory inside the container
WORKDIR /app

# Copy the published app from the build stage
COPY --from=build /app/out .

# Expose the port the app runs on
EXPOSE 5000

# Start the app
ENTRYPOINT ["dotnet", "SmsControl.dll"]