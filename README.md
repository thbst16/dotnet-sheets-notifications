# dotnet-sheets-notifications

[![Build Status](https://beckshome.visualstudio.com/dotnet-sheets-notifications/_apis/build/status/thbst16.dotnet-sheets-notifications?branchName=main)](https://beckshome.visualstudio.com/dotnet-sheets-notifications/_build/latest?definitionId=9&branchName=main)
![Docker Image Version (latest by date)](https://img.shields.io/docker/v/thbst16/azure-function-notification?logo=docker)
![Uptime Robot ratio (7 days)](https://img.shields.io/uptimerobot/ratio/7/m790221273-210b408ec2755d217c4e5e5b?logo=http) 

User-friendly solution for scheduling and receiving notifications for daily events. Just plug in scheduled events using Google Sheets on the web. Phone calls, text messages and emails with the messages of your choice are scheduled and sent to the number / email address that you specify.

![Notification Triggers Sheet](https://s3.amazonaws.com/s3.beckshome.com/20210316-notification-triggers-sheet.jpg)

# Functional Flow
The dotnet-sheets-notifications solution orchestrates the integration of services to support the sending of automated messages, as illustrated in the figure below.

![Notification Flow](https://s3.amazonaws.com/s3.beckshome.com/20220102-dotnet-sheets-notifications-functional.jpg)

# Notional Architecture
The figure bellow represents the notional architecture of dotnet-sheets-notifications. The image is docker-based and therefore highly portable. The solution takes a number of dependencies on external services. These services are configured through configuration files (secrets) that are stored seperate from the container and bound at runtime.

![Notification Flow](https://s3.amazonaws.com/s3.beckshome.com/20220102-dotnet-sheets-notifications-notional-architecture.jpg)

Specific statistics and outputs of interest would be:
* [Azure DevOps Build and Deploy Pipeline and Status](https://beckshome.visualstudio.com/dotnet-sheets-notifications/_build?definitionId=9)
* [Docker Hub image repository for the Solution](https://hub.docker.com/repository/docker/thbst16/azure-function-notification)
* [Public HTTP Health Check for the Solution](https://dotnet-sheets-notification.azurewebsites.net/)
# Configuration

The program requires specific configurations to work for your accounts and situation. The highlights of these configurations are covered below.

* **Google Sheets**
  * Spreadsheet Format - The spreadsheet must apply the exact headers shown in the image above: Date, Time, Type, Destination, Message, and Processed.
  * Processing - Messages are processed as soon as the date / time passes for rows where the processed field is set to "FALSE". The processed field is updated by the program when the message is processed.
  * Message Types - The 3 types of message are: PHONE, SMS and EMAIL.
  * Spreadsheet Tab Name - Set to "TiggerList" using the static readonly variable 'sheet' in GoogleSheetTrigger.cs.
  * Spreadsheet ID - Set as a property in local.settings.json. The Google Sheets spreadsheet ID can be found in the Sheets URL.
  * Permissions - Permissions need to be granted to a service account to update the spreadsheet. This access then needs to be exported as a client_secrets.json file from Google Sheets and imported into the project.
* **Azure**
  * Google Secrets - The Google client_secrets.json file should not be shared or made publicly accessible. This can be shared as a Secure File in Azure DevOps and accessed using the DownloadSecureFile task.
  * Config Values - Configuration values stored in appsettings.json and are available through dynamic configuration in the progra.
* **Code**
  * Spreadsheet Tab Name - As mentioned earlier, the spreadsheet tab name can be set using the static readonly variable 'sheet' in GoogleSheetTrigger.cs.
  * Timing - The timing is set staticly in a TimerTrigger CronTab value.
  * Time Zones - Processing times are currently set in EST. Times are baselined to UTC and can be set to your timezone with the static variable estZone in GoogleSheetTrigger.cs. Timezone settings are platform agnostic -- either Windows (e.g. "Eastern Standard Time") or IANA settings (e.g. "America/New York") can be used.
* **Subscriptions**
  * Accounts - Active Azure, Twilio and SendGrid subscriptions and credentials are required. Trial accounts will work for this purpose.
  * Credentials - Subscription credentials are stored in the appsettings.json file (locally) or Application Settings in Azure