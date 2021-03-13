using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        static readonly string ApplicationName = "NotificationTriggers";
        static readonly string SpreadsheetId = "187QFg9LDDsBYxsRs1ON3xHJLr_4viFOyE8C3GOwLnNI";
        static readonly string sheet = "TriggerList";
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
            //CreateEntry();
            //log.LogInformation($"New Google Sheet row created at: {DateTime.Now}");
            //System.Threading.Thread.Sleep(1000);
            //UpdateEntry();
            //log.LogInformation($"Google Sheet row updated at: {DateTime.Now}");
            log.LogInformation($"Google Sheet row read -- value: " + ReadEntries());
            log.LogInformation("************************************************");
        }

        static String ReadEntries(){
            var range = $"{sheet}";
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);

            var response = request.Execute();
            var values = response.Values;
            StringBuilder sb = new StringBuilder();
            if (values != null && values.Count > 0)
            {
                foreach(var (row, index) in values.WithIndex())
                {
                    sb.Append("\n");
                    sb.Append(row[0]);
                    sb.Append(" | ");
                    sb.Append(row[1]);
                    sb.Append(" | ");
                    sb.Append(row[2]);
                    sb.Append(" | ");
                    sb.Append(row[3]);
                    sb.Append(" | ");
                    sb.Append(row[4]);
                    sb.Append(" | ");
                    sb.Append(row[5]);
                    if (
                            row[0].ToString().ToUpper() != "DATE" &&
                            DateTime.Parse(row[0].ToString()).Date == DateTime.Now.Date &&
                            DateTime.Parse(row[1].ToString()).TimeOfDay < DateTime.Now.TimeOfDay &&
                            row[5].ToString().ToUpper() == "FALSE"
                        )
                    {
                        sb.Append("\n");
                        sb.Append("==> ");
                        sb.Append(row[2]);
                        sb.Append(" to ");
                        sb.Append(row[3]);
                        sb.Append("; index = F");
                        sb.Append(index+1);
                    }
                }
                return(sb.ToString());
            }
            else
            {
                return("No data found");
            }
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)       
            => self.Select((item, index) => (item, index));   

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
