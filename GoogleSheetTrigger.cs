using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace com.beckshome.function
{
    public static class GoogleSheetTrigger
    {
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "BeckTest";
        static readonly string SpreadsheetId = "1KxE0mHeRoL5T7j1UBfNjXPKyPVW3OvrWsesHsn5xHjQ";
        static readonly string sheet = "congress";
        static SheetsService service;
        
        [FunctionName("GoogleSheetTrigger")]
        public static void Run([TimerTrigger("*/60 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Google Sheets Trigger executed at: {DateTime.Now}");

            GoogleCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(Scopes);
            }

            service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer(){
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            log.LogInformation("************************************************");
            CreateEntry();
            log.LogInformation($"New Google Sheet row created at: {DateTime.Now}");
            System.Threading.Thread.Sleep(1000);
            UpdateEntry();
            log.LogInformation($"Google Sheet row updated at: {DateTime.Now}");
            log.LogInformation($"Google Sheet row read -- value: " + ReadEntries());
            log.LogInformation("************************************************");
        }

        static String ReadEntries(){
            var range = $"{sheet}!E5:F5";
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);

            var response = request.Execute();
            var values = response.Values;
            StringBuilder sb = new StringBuilder();
            if (values != null && values.Count > 0)
            {
                foreach(var row in values)
                {
                    sb.Append(row[1]);
                    sb.Append(row[0]);
                }
                return(sb.ToString());
            }
            else
            {
                return("No data found");
            }
        }

        static void CreateEntry()
        {
            var range = $"{sheet}!A:F";
            var valueRange = new ValueRange();

            var objectList = new List<object>() {"Hello!", "This", "was", "inserted", "at", DateTime.Now.ToLocalTime()};
            valueRange.Values = new List<IList<object>> {objectList};

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var appendResponse = appendRequest.Execute();
        }

        static void UpdateEntry()
        {
            var range = $"{sheet}!D543";
            var valueRange = new ValueRange();

            var objectList = new List<object>() { "updated" };
            valueRange.Values = new List<IList<object>> { objectList };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var updateResponse = updateRequest.Execute();
        }

        static void DeleteEntry()
        {
            var range = $"{sheet}!A543:F";
            var requestBody = new ClearValuesRequest();

            var deleteRequest = service.Spreadsheets.Values.Clear(requestBody, SpreadsheetId, range);
            var deleteResponse = deleteRequest.Execute();
        }
    }
}
