using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace com.beckshome.function
{
    public static class BeerTrigger
    {
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
