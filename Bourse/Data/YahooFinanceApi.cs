using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Bourse.Data
{
    public class YahooFinanceApi
    {
        //private string crumb;
        private CookieContainer cookies;

        public YahooFinanceApi()
        {
            cookies = new CookieContainer();
        }

        public async Task<string> DownloadHistoricalDataAsync(string symbol, long period1, long period2, string interval)
        {
            //if (string.IsNullOrEmpty(crumb))
            //{
            //    await GetCrumbAndCookies(symbol);
            //}

            var url = $"https://query1.finance.yahoo.com/v7/finance/download/{symbol}?period1={period1}&period2={period2}&interval={interval}&events=history";
            
            using (var handler = new HttpClientHandler { CookieContainer = cookies })
            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMinutes(15); // Augmente le délai à 5 minutes

                try
                {
                    var response = await client.GetAsync(url);

                    if ((int)response.StatusCode == 429)
                    {
                        Console.WriteLine("Trop de requêtes - Attente de 2 secondes...");
                        await Task.Delay(2000); // Attendre 2 secondes avant de réessayer
                        response = await client.GetAsync(url); // Réessayer la requête après la pause
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Erreur HTTP : {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }

                    return await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException ex) when ((int?)ex.StatusCode == 429)
                {
                    Console.WriteLine("Limite de taux atteinte. Essayez d'attendre un peu.");
                    // Attendre plus longtemps ou implémenter une logique de réessai exponentielle
                    await Task.Delay(5000); // Attente de 5 secondes par exemple
                    return await client.GetStringAsync(url); // Réessayer
                }
            }
        }

        //private async Task GetCrumbAndCookies(string symbol)
        //{
        //    using (var handler = new HttpClientHandler { CookieContainer = cookies })
        //    using (var client = new HttpClient(handler))
        //    {
        //        client.Timeout = TimeSpan.FromMinutes(5); // Augmente le délai à 5 minutes

        //        var url = $"https://finance.yahoo.com/quote/{symbol}/history";
        //        var response = await client.GetStringAsync(url);

        //        var crumbMatch = Regex.Match(response, "\"CrumbStore\":\\{\"crumb\":\"(?<crumb>[^\"]+)\"\\}");
        //        if (!crumbMatch.Success)
        //        {
        //            throw new Exception("Unable to extract crumb from the response.");
        //        }

        //        crumb = crumbMatch.Groups["crumb"].Value;
        //    }
        //}
    }
}
