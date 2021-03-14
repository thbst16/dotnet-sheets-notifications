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
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace com.beckshome.function
{
    public static class GoogleSheetTrigger
    {
        // Static values bound to the specific Google Sheet and Tab
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "NotificationTriggers";
        static readonly string SpreadsheetId = "187QFg9LDDsBYxsRs1ON3xHJLr_4viFOyE8C3GOwLnNI";
        static readonly string sheet = "TriggerList";
        static SheetsService service;
        
        // Azure trigger function to access Google sheets and process triggers
        [FunctionName("GoogleSheetTrigger")]
        public static void Run([TimerTrigger("*/30 * * * * *")]TimerInfo myTimer, ILogger log)
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
            log.LogInformation(ProcessTriggers());
            log.LogInformation("************************************************");
        }

        static String ProcessTriggers(){
            var range = $"{sheet}";
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);

            var response = request.Execute();
            var values = response.Values;

            StringBuilder sb = new StringBuilder();
            if (values != null && values.Count > 0)
            {
                foreach(var (row, index) in values.WithIndex())
                {
                    sb.Append($"\n {row[0]} | {row[1]} | {row[2]} | {row[3]} | {row[4]} | {row[5]}");
                    // Conditional logic: (1) not header; (2) today; (3) in past; (4) not yet processed
                    if (
                            row[0].ToString().ToUpper() != "DATE" &&
                            DateTime.Parse(row[0].ToString()).Date == DateTime.Now.Date &&
                            DateTime.Parse(row[1].ToString()).TimeOfDay < DateTime.Now.TimeOfDay &&
                            row[5].ToString().ToUpper() == "FALSE"
                        )
                    {
                        sb.Append($"\n*** {row[2]} to {row[3]} ***");
                        // Call the communication function to send the communications
                        string sid = SendCommunication(row[2].ToString(), row[3].ToString(), row[4].ToString());
                        // Set update row cell and call update function
                        string updater = $"{sheet}!F" + (index+1);
                        UpdateProcessedDetails(updater, sid);
                    }
                }
                return(sb.ToString());
            }
            else
            {
                return("No data found");
            }
        }

        // Small extension function to allow indexing within foreach statement
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)       
            => self.Select((item, index) => (item, index));   

        // Marks the specified row (cell) with the current time as processed
        static void UpdateProcessedDetails(string rowToUpdate, string sid)
        {
            var range = rowToUpdate;
            var valueRange = new ValueRange();

            var objectList = new List<object>() { DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") + $" SID: {sid}"};
            valueRange.Values = new List<IList<object>> { objectList };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var updateResponse = updateRequest.Execute();
        }

        // Send communications via Twilio API and return the completed communication SID
        static string SendCommunication(string type, string destination, string message)
        {
            string accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
            string authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
            string phoneNumber = Environment.GetEnvironmentVariable("TWILIO_NUMBER");
            string sendGridKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            string sendGridFrom= Environment.GetEnvironmentVariable("SENDGRID_FROM");
            string sendGridTo = Environment.GetEnvironmentVariable("SENDGRID_TO");
            string sendGridName = Environment.GetEnvironmentVariable("SENDGRID_NAME");

            TwilioClient.Init(accountSid, authToken);
            var sendGridClient = new SendGridClient(sendGridKey);

            if (type.ToUpper() == "SMS")
            {
                var msg = MessageResource.Create(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(phoneNumber),
                    to: new Twilio.Types.PhoneNumber(destination)
                );
                return msg.Sid;
            }
            else if (type.ToUpper() == "PHONE")
            {
                var call = CallResource.Create(
                    twiml: new Twilio.Types.Twiml($"<Response><Say>{message}</Say></Response>"),
                    to: new Twilio.Types.PhoneNumber(destination),
                    from: new Twilio.Types.PhoneNumber(phoneNumber)
                );
                return call.Sid;
            }
            else if (type.ToUpper() == "EMAIL")
            {
                var from = new EmailAddress(sendGridFrom, sendGridName);
                var subject = "Automated Trigger Message";
                var to = new EmailAddress(sendGridTo, sendGridName);
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
    }
}
