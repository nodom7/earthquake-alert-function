using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;

public static class QuakeAlertFunction
{
    [FunctionName("QuakeAlertFunction")]
    public static async Task Run(
        [BlobTrigger("quakejsondata/{name}", Connection = "AzureWebJobsStorage")] Stream myBlob,
        string name,
        ILogger log)
    {
        log.LogInformation($"C# Blob trigger function processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

        string jsonContent;
        using (StreamReader reader = new StreamReader(myBlob))
        {
            jsonContent = await reader.ReadToEndAsync();
        }

        JObject earthquakeData = JObject.Parse(jsonContent);

        string place = earthquakeData["properties"]["place"].ToString();
        double magnitude = (double)earthquakeData["properties"]["mag"];
        long timestamp = (long)earthquakeData["properties"]["time"];

        if (magnitude > 5.0)
        {
            string alertMessage = $"Earthquake Alert: Magnitude {magnitude} earthquake detected near {place} at {DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("g")}";

            await SendEmailAlert(alertMessage, log);
            log.LogInformation($"Alert sent: {alertMessage}");
        }
    }

    private static async Task SendEmailAlert(string alertMessage, ILogger log)
    {
        var apiKey = Environment.GetEnvironmentVariable("SendGridApiKey");
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress("alerts@yourdomain.com", "Earthquake Alerts");
        var subject = "Earthquake Alert";
        var to = new EmailAddress("recipient@example.com", "Alert Recipient");
        var plainTextContent = alertMessage;
        var htmlContent = $"<strong>{alertMessage}</strong>";
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        log.LogInformation($"Email sent. Status Code: {response.StatusCode}");
    }
}