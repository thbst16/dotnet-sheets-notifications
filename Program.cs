using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace dotnet_sheets_notifications
{
    static class Program
    {
        // Static values bound to the specific Google Sheet and Tab
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets};
        static readonly string ApplicationName = "dotnet-sheets-notifications";
        static SheetsService sheetService;
        static TimeZoneInfo estZone = TimeZoneConverter.TZConvert.GetTimeZoneInfo("Eastern Standard Time");
        static void Main(string[] args)
        {
            // Setup and retrieve basic job settings
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

            // Set spreadsheet and document ID
            GoogleSettings googleSettings = config.GetRequiredSection("Google").Get<GoogleSettings>();
            string SpreadsheetId = googleSettings.SpreadsheetId;
            string sheet = googleSettings.Sheet;
            
            // Setup logging configuration
            LoggingSettings loggingSettings = config.GetRequiredSection("AzureLog").Get<LoggingSettings>();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.AzureAnalytics(loggingSettings.WorkspaceId, loggingSettings.PrimaryKey, "DotNetSheetsNotifications", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Establish Google Credential
            GoogleCredential credential;
            
            // Setup Access for Sheets Service
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(Scopes);

                sheetService = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer(){
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });
            }
            
            // Execute master loop for communications job
            Log.Information("Program started at " + DateTime.Now);
            while (true)
            { 
                try
                {
                    System.Threading.Thread.Sleep(1000 * settings.JobFrequencySeconds);
                    var debugString = ProcessTriggers(sheetService, sheet, SpreadsheetId);
                    Log.Debug(debugString);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Issue with the program: " + ex);
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            }
            
        }
        static string ProcessTriggers(SheetsService service, string sheet, string spreadsheetId)
        {
            // Setup and retrieve basic job settings
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

            // Setup logging configuration
            LoggingSettings loggingSettings = config.GetRequiredSection("AzureLog").Get<LoggingSettings>();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.AzureAnalytics(loggingSettings.WorkspaceId, loggingSettings.PrimaryKey, "DotNetSheetsNotifications", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
            // Execute get request to get all spreadsheet values for processing
            var range = $"{sheet}";
            var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;

            StringBuilder sb = new StringBuilder();
            if (values != null && values.Count > 0)
            {
                // Preamble
                Log.Information("Dotnet Sheets Notification trigger executed at " + DateTime.Now);
                sb.Append("\n************************************************");
                foreach(var (row, index) in values.WithIndex())
                {
                    sb.Append($"\n {row[0]} | {row[1]} | {row[2]} | {row[3]} | {row[4]} | {row[5]}");
                    // Conditional logic: (1) not header; (2) today; (3) in past; (4) not yet processed
                    if (
                            row[0].ToString().ToUpper() != "DATE" &&
                            DateTime.Parse(row[0].ToString()).Date == (TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, estZone)).Date &&
                            DateTime.Parse(row[1].ToString()).TimeOfDay < (TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, estZone)).TimeOfDay &&
                            row[5].ToString().ToUpper() == "FALSE"
                        )
                    {
                        // Log communication executed
                        sb.Append($"\n*** {row[2]} to {row[3]} ***");
                        Log.Information("Sent " + $"{row[2]} to {row[3]}");
                        string sid = SendCommunication(row[2].ToString(), row[3].ToString(), row[4].ToString());
                        string updater = $"{sheet}!F" + (index+1);
                        UpdateProcessedDetails(service, spreadsheetId, updater, sid);
                    }
                }
                // Time logging + postamble
                sb.Append($"\n System date: {(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, estZone)).Date.ToString("MMMM dd, yyyy")}");
                sb.Append($"\n System time: {(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, estZone)).TimeOfDay.ToString("hh\\:mm\\:ss")}");
                sb.Append("\n************************************************\n");
                return sb.ToString();
            }
            else
            {
                return("No data was found.");
            }
        }

        // Small extension function to allow indexing within foreach statement
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)       
            => self.Select((item, index) => (item, index)); 

        static void UpdateProcessedDetails(SheetsService service, string SpreadsheetId, string rowToUpdate, string sid)
        {
            var range = rowToUpdate;
            var valueRange = new ValueRange();

            var objectList = new List<object>() { TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, estZone).ToString("MM/dd/yyyy hh:mm:ss tt") + $" SID: {sid}"};
            valueRange.Values = new List<IList<object>> { objectList };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var updateResponse = updateRequest.Execute();
        }

        // Send communications via Twilio API and return the completed communication SID
        static string SendCommunication(string type, string destination, string message)
        {
            // Setup and retrieve communications settings
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            EmailSettings emailSettings = config.GetRequiredSection("Sendgrid").Get<EmailSettings>();
            SmsSettings smsSettings = config.GetRequiredSection("Twilio").Get<SmsSettings>();

            TwilioClient.Init(smsSettings.AccountSid, smsSettings.AuthToken);
            var sendGridClient = new SendGridClient(emailSettings.ApiKey);

            if (type.ToUpper() == "SMS")
            {
                var msg = MessageResource.Create(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(smsSettings.FromNumber),
                    to: new Twilio.Types.PhoneNumber(destination)
                );
                return msg.Sid;
            }
            else if (type.ToUpper() == "PHONE")
            {
                var call = CallResource.Create(
                    twiml: new Twilio.Types.Twiml("<Response><Say voice=\"Polly.Joanna-Neural\"><break time=\"600ms\"/>{message}<break time=\"600ms\"/></Say></Response>"),
                    to: new Twilio.Types.PhoneNumber(destination),
                    from: new Twilio.Types.PhoneNumber(smsSettings.FromNumber)
                );
                return call.Sid;
            }
            else if (type.ToUpper() == "EMAIL")
            {
                var from = new EmailAddress(emailSettings.FromEmail, emailSettings.Name);
                var subject = "Automated Trigger Message";
                var to = new EmailAddress(destination, emailSettings.Name);
                var plainTextContent = message;
                var htmlContent = message;
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                sendGridClient.SendEmailAsync(msg);
                return "Email";
            }
            else
            {
                return "-1";
            }
        }

        // Master job settings
        public class Settings
        {
            public int JobFrequencySeconds { get; set; }
        }

        public class GoogleSettings
        {
            public string SpreadsheetId { get; set; }
            public string Sheet { get; set; }
        }

        public class LoggingSettings
        {
            public string WorkspaceId { get; set; }
            public string PrimaryKey { get; set; }
        }

        public class EmailSettings
        {
            public string ApiKey{ get; set; }
            public string Name { get; set; }
            public string FromEmail { get; set; }
        }

        public class SmsSettings
        {
            public string AccountSid { get; set; }
            public string AuthToken { get; set; }
            public string FromNumber { get; set; }
        }
    }
}
