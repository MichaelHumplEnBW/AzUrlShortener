using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using Cloud5mins.domain;
using Microsoft.Extensions.Configuration;

namespace Cloud5mins.Function
{
    public static class UrlHoldUpRedirect
    {
        [FunctionName("UrlHoldUpRedirect")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "UrlHoldUpRedirect/{shortUrl}")] HttpRequestMessage req,
            string shortUrl, 
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed for Url: {shortUrl}");

            string redirectUrl = "https://azure.com";

            if (!String.IsNullOrWhiteSpace(shortUrl))
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                redirectUrl = config["defaultRedirectUrl"];

                StorageTableHelper stgHelper = new StorageTableHelper(config["UlsDataStorage"]); 

                var tempUrl = new ShortUrlEntity(string.Empty, shortUrl);
                
                var newUrl = await stgHelper.GetShortUrlEntity(tempUrl);

                if (newUrl != null)
                {
                    log.LogInformation($"Found it: {newUrl.Url}");
                    newUrl.Clicks++;
                    stgHelper.SaveClickStatsEntity(new ClickStatsEntity(newUrl.RowKey));
                    await stgHelper.SaveShortUrlEntity(newUrl);
                    redirectUrl = newUrl.Url
                    ;
                }
            }
            else
            {
                log.LogInformation("Bad Link, resorting to fallback.");
            }

            var res = req.CreateResponse(HttpStatusCode.OK,"Der Link führt zu <a href=\""+redirectUrl+"\">"+WebUtility.HtmlEncode(redirectUrl)+"</a>");
            return res;
        }
  }
}
