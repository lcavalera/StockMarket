using Bourse.Data;
using Bourse.Interfaces;
using Bourse.Models;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.Playwright;

namespace Bourse.Services
{
    public class IndiceService : IIndiceService
    {
        private readonly IServiceProvider _serviceProvider;

        public IndiceService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<List<Indice>> ObtenirTout()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();

                return await dbContext.Indices.Include(i=> i.TrainingData).ToListAsync();
            }
        }

        public async Task<List<Indice>> ObtenirSelonName(string name)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();
                return await dbContext.Indices.Include(i => i.TrainingData).Where(i => i.Symbol.ToUpper().Contains(name.ToUpper()) || i.Name.ToLower().Contains(name.ToLower())).ToListAsync();
            }
        }

        public async Task<Indice> ObtenirSelonSymbol(string symbol)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();

                return await dbContext.Indices.Include(i => i.TrainingData).SingleOrDefaultAsync(i => i.Symbol == symbol);
            }
        }

        public async Task<Indice> ObtenirSelonId(int id)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();

                return await dbContext.Indices.Include(i => i.TrainingData).SingleOrDefaultAsync(i => i.Id == id);
            }
        }

        public async Task GetImageAnalysisIndice(int id)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();

                Indice indice = await ObtenirSelonId(id);

                try
                {
                    string indiceSymbol = indice.Symbol;
                    string url = $"https://finance.yahoo.com/quote/{indiceSymbol}/analysis/";

                    Console.WriteLine($"Navigating to: {url}");

                    int retryCount = 3;
                    for (int attempt = 1; attempt <= retryCount; attempt++)
                    {
                        try
                        {
                            using (var playwright = await Playwright.CreateAsync())
                            {
                                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                                {
                                    Headless = true
                                });

                                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                                {
                                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36"
                                });

                                var page = await context.NewPageAsync();

                                var response = await page.GotoAsync(url, new PageGotoOptions
                                {
                                    Timeout = 10000, // 10 secondes
                                    WaitUntil = WaitUntilState.DOMContentLoaded
                                });

                                if (response != null || response.Ok)
                                {
                                    var sectionExists = await page.Locator("section[data-testid='analyst-recommendations-card']").IsVisibleAsync();
                                    if (sectionExists)
                                    {
                                        var canvasLocator = page.Locator("section[data-testid='analyst-recommendations-card'] canvas");

                                        await canvasLocator.WaitForAsync(new LocatorWaitForOptions
                                        {
                                            Timeout = 10000, // 10 secondes
                                            State = WaitForSelectorState.Visible
                                        });

                                        // Obtenez l'élément avec `ElementHandleAsync`
                                        var canvas = await canvasLocator.ElementHandleAsync();
                                        if (canvas != null)
                                        {
                                            // Attendre que le canvas soit visible et stable
                                            await canvas.WaitForElementStateAsync(ElementState.Visible);
                                            await canvas.WaitForElementStateAsync(ElementState.Stable);

                                            // Prendre la capture d'écran
                                            var screenshot = await canvas.ScreenshotAsync(new ElementHandleScreenshotOptions
                                            {
                                                Type = ScreenshotType.Png,
                                                Timeout = 10000 // 10 secondes
                                            });

                                            // Convertir en base64
                                            string base64Image = Convert.ToBase64String(screenshot);

                                            // Ajout image Analysis
                                            indice.imageAnalysis = Convert.FromBase64String(base64Image);
                                        }
                                        Console.WriteLine("Canvas introuvable !");
                                    }
                                    Console.WriteLine("Section Analyst Recommendations introuvable.");
                                }

                                Console.WriteLine($"Erreur : Page introuvable ou inaccessible pour {indice.Symbol}.");
                                await page.CloseAsync();
                                await context.CloseAsync();
                                await browser.CloseAsync();
                                break;
                            }
                        }
                        catch (PlaywrightException ex) when (ex.Message.Contains("Process exited"))
                        {
                            Console.WriteLine($"Attempt {attempt + 1}: Browser process exited unexpectedly. Retrying...");
                            await Task.Delay(5000); // Attente avant réessai
                        }
                        catch (TimeoutException ex)
                        {
                            Console.WriteLine($"Tentative {attempt}/{retryCount} échouée : {ex.Message}");
                            if (attempt == retryCount)
                                throw;
                        }
                    }
                    dbContext.Indices.Update(indice);
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la récupération de l'analyse pour l'indice {indice.Symbol}. Exception: {ex.Message}");
                }
            }
        }
    }
}
