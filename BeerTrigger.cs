using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace com.beckshome.function
{
    public static class BeerTrigger
    {
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "BeckTest";
        static readonly string SpreadsheetId = "1KxE0mHeRoL5T7j1UBfNjXPKyPVW3OvrWsesHsn5xHjQ";
        static readonly string sheet = "congress";
        static SheetsService service;
        
        [FunctionName("HelloTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req, ILogger log)
        {
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

            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            
            string requestBody = String.Empty;
            using (StreamReader streamReader =  new  StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            CreateEntry();
            
            return name != null
                ? (ActionResult)new OkObjectResult(ReadEntries())
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
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

            var objectList = new List<object>() {"Hello!", "This", "was", "inserted", "via", "C#"};
            valueRange.Values = new List<IList<object>> {objectList};

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var appendResponse = appendRequest.Execute();
        }
        
        /*
        [FunctionName("BeerTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            var breweryDbApiKey = Environment.GetEnvironmentVariable("BREWERY_DB_API_KEY");
            var client = new HttpClient();
            var response = await client.GetAsync($"https://sandbox-api.brewerydb.com/v2/beer/random?key={breweryDbApiKey}");
            var responseString = await response.Content.ReadAsStringAsync();
            var responseRoot = JsonConvert.DeserializeObject<Root>(responseString);

            return (ActionResult)new OkObjectResult(responseRoot.Data);
        }
        */
    }

    public class Root
    {
        public string Message { get; set; }
        public Beer Data { get; set; }
        public bool Success { get; set; }
    }

    public class Beer
    {
        public string Name { get; set; }
        public string ABV { get; set; }
        public Style Style { get; set; }
    }

    public class Style
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
