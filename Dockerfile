# use an official microsoft SDK as a parent image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

# set our working directory to /app
WORKDIR /app

# copy the working directory contents into the container at /app
COPY . .
# copy a dummy appsettings.json file in, since this file is secret
COPY config/appsettings-sample.json /app/config/appsettings.json
# copy a dummy client_secrets.json file in, since this file is secret
COPY config/client_secrets-sample.json /app/config/client_secrets.json

# build and put files in a folder called output
RUN dotnet build -c Release -o output

# Build runtime image. Note use of runtime version of core image to reduce size of final image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime-env
WORKDIR /app
COPY --from=build-env app/output .

# Once the container launches, run the console
ENTRYPOINT ["dotnet", "dotnet-sheets-notifications.dll"]