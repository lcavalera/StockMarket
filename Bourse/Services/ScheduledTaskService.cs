using Bourse.Data;
using Bourse.Interfaces;
using Bourse.Models;
using Flurl.Http;
using HtmlAgilityPack;
using Microsoft.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using System.Data;
using System.Globalization;
using System.Text;

public class ScheduledTaskService : BackgroundService, IScheduledTaskService
{
    private readonly IServiceScopeFactory _scopeFactory;
    //private readonly IConfiguration _config;
    //private CookieContainer _cookies;
    private readonly MLContext _mlContext;
    private readonly string[] _filePath;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 5);
    //private readonly string _apiKey;

    public ScheduledTaskService(IServiceScopeFactory scopeFactory, MLContext mlContext)
    {
        _mlContext = mlContext;
        //_cookies = new CookieContainer();
        _scopeFactory = scopeFactory;
        _filePath = ["TSX.txt", "NASDAQ.txt", "AMEX.txt", "NYSE.txt"]; //Ajouter autres bourse si necessaire
        //_config = config;
        //_apiKey = "JLQRQLBRERE2WPSA"; // Remplacez par votre clé API Alpha Vantage
    }

    // NOTE : AJOUTER JOURNALISATION SERILOG ou NLOG

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //// Créer un navigateur Playwright une seule fois
        //using (var playwright = await Playwright.CreateAsync())
        //{
        //    var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        //    {
        //        Headless = true
        //    });

        //    var context = await browser.NewContextAsync(new BrowserNewContextOptions
        //    {
        //        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36"
        //    });

        //    var page = await context.NewPageAsync();

        // Chaque bourse est gérée dans une tâche dédiée via Task.Run
        var tasks = _filePath.Select(async bourse =>
            {
                try
                {
                    await GérerBourse(bourse, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur pour {bourse} : {ex.Message}");
                    // Vous pouvez aussi logger avec _logger si disponible
                }
            }).ToList();

            await Task.WhenAll(tasks);

        //    // Fermer le navigateur après toutes les tâches
        //    await page.CloseAsync();
        //    await context.CloseAsync();
        //    await browser.CloseAsync();
        //}
    }

    private async Task GérerBourse(string bourse, CancellationToken stoppingToken)
    {
        string nomBourse = Path.GetFileNameWithoutExtension(bourse);
        Console.WriteLine($"Démarrage des tâches pour {nomBourse}...");

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                BourseContext dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();

                // Charger les indices
                var indices = await dbContext.Indices
                    .Include(i => i.TrainingData)
                    .AsNoTracking()
                    .Where(i => i.Bourse == nomBourse)
                    .ToListAsync();

                if (!indices.Any())
                {
                    Console.WriteLine($"Aucun indice trouvé pour {nomBourse}. Récupération des symboles...");
                    indices = (await ObtenirSymbols(bourse))?.ToList() ?? new List<Indice>();

                    if (!indices.Any())
                    {
                        Console.WriteLine($"Aucun symbole récupéré pour {nomBourse}. Utilisation d'une liste vide.");
                    }
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    // Charger les horaires de la bourse
                    FuseHoraire fuseHoraire = GetHoraireOverture(nomBourse.ToLower());
                    // Convertir la date donnée dans le fuseau horaire de la bourse
                    DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

                    if (EstDansLesHorairesBourse(nomBourse.ToLower()))
                    {
                        // Vérifier si nous sommes lundi
                        if (dateDansLeFuseau.DayOfWeek == DayOfWeek.Monday)
                        {
                            // 🔹 Étape 1 : Bourse ouverte, on met à jour les earnings
                            Console.WriteLine($"Bourse {nomBourse} ouverte. Mise à jour des dates financières...");
                            await UpdateEarningDates(nomBourse, stoppingToken);
                        }

                        // 🔹 Attendre la fermeture de la bourse
                        Console.WriteLine($"Attente de la fermeture du marché pour {nomBourse}...");
                        await WaitForEndOfDay(stoppingToken, fuseHoraire);
                    }
                    else
                    {
                        // 🔹 Étape 2 : Attendre la fin de la journée si on est dans un jour boursiere

                        // Vérifier si c'est un jour ouvré (lundi à vendredi) et non un jour férié
                        if (dateDansLeFuseau.DayOfWeek != DayOfWeek.Saturday && dateDansLeFuseau.DayOfWeek != DayOfWeek.Sunday)
                        {
                            // 🔹 Vérifier si nous sommes toujours le même jour avant d'attendre la fin de journée
                            if ((dateDansLeFuseau.Hour > 16 && dateDansLeFuseau.Hour < 23) 
                                || (dateDansLeFuseau.Hour == 23 && dateDansLeFuseau.Minute < 59))
                            {
                                Console.WriteLine($"Attente de la fin de la journée pour {nomBourse}...");
                                await WaitForEndOfDay(stoppingToken, fuseHoraire);
                            }
                            else
                            {
                                Console.WriteLine($"Minuit dépassé. Passer directement à la prochaine mise à jour des earnings...");
                            }
                        }

                        // 🔹 Étape 3 : Sauvegarde de l'historique manquant
                        Console.WriteLine($"Sauvegarde des données historiques pour {nomBourse}...");
                        await SauvegarderHistoriqueManquant(nomBourse, fuseHoraire, stoppingToken);

                        // 🔹 Étape 4 : Bourse fermée, on gère les indices
                        Console.WriteLine($"Bourse {nomBourse} fermée. Lancement de la gestion des indices...");

                        await Parallel.ForEachAsync(indices, stoppingToken, async (indice, token) =>
                        {
                            if (indice.DateUpdated == null || indice.DateUpdated < dateDansLeFuseau)
                            {
                                await GérerIndice(indice, nomBourse, fuseHoraire, token);
                            }
                        });

                        //var tasks = indices
                        //    .Where(indice => indice.DateUpdated == null || indice.DateUpdated < dateDansLeFuseau) // Filtrer les indices
                        //    .Select(indice =>
                        //    GérerIndice(indice, nomBourse, dbContext, page, fuseHoraire, stoppingToken)).ToList();

                        //await Task.WhenAll(tasks);

                        // 🔹 Étape 5 : Attendre l'ouverture de la bourse
                        Console.WriteLine($"Marché fermé pour {nomBourse}. En attente de l'ouverture...");
                        await WaitUntilMarketOpens(stoppingToken, fuseHoraire);
                    }
                }

                //// Verifier si on est dans l'horaire de la bourse correspondant
                //if (!EstDansLesHorairesBourse(nomBourse.ToLower()))
                //{
                //    // Vérifier si minuit n'est pas encore passé
                //    if (DateTime.Now >= DateTime.Today.Add(fuseHoraire.Fermeture) && DateTime.Now < DateTime.Today.AddDays(1))
                //    {
                //        // Faire la mise à jour des dates financieres
                //        await UpdateEarningDates(nomBourse, stoppingToken);

                //        // Appeler la méthode pour attendre la fin de la journée
                //        await WaitForEndOfDay(stoppingToken, fuseHoraire);
                //    }

                //    // Faire la mise à jour des dates financieres
                //    await UpdateEarningDates(nomBourse, stoppingToken);

                //    // Faire la sauvegarde de la journée
                //    await SauvegarderHistoriqueManquant(nomBourse, fuseHoraire, stoppingToken);

                //    // Attendre l'ouverture de la bourse
                //    Console.WriteLine($"Marché fermé pour {nomBourse}, en attente...");
                //    await WaitUntilMarketOpens(stoppingToken, fuseHoraire);
                //}

                //await SauvegarderHistoriqueManquant(nomBourse, fuseHoraire, stoppingToken);

                //Console.WriteLine($"Bourse {nomBourse} ouverte. Tâches démarrées pour {indices.Count} indices.");

                //var tasks = indices.Select(indice => GérerIndice(indice, nomBourse, dbContext, page, fuseHoraire, stoppingToken)).ToList();

                //await Task.WhenAll(tasks);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur pour {nomBourse} : {ex.Message}");
        }
    }
    private async Task GérerIndice(Indice indice, string nomBourse, FuseHoraire fuseHoraire, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 🔹 Vérifier si la bourse est ouverte
            if (EstDansLesHorairesBourse(nomBourse.ToLower()))
            {
                Console.WriteLine($"Marché ouvert pour {indice.Name}. Attente de la fermeture...");

                // On attend la fermeture avant de recommencer le cycle
                await WaitForEndOfDay(stoppingToken, fuseHoraire);
                continue;
            }

            try
            {
                // 🔹 Attente d'accès au sémaphore pour éviter trop de tâches concurrentes
                await _semaphore.WaitAsync(stoppingToken);

                // Convertir la date donnée dans le fuseau horaire de la bourse
                DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

                if (indice.DateUpdated.AddDays(1).DayOfWeek != DayOfWeek.Saturday && indice.DateUpdated.AddDays(1).DayOfWeek != DayOfWeek.Sunday && !fuseHoraire.JoursFeries.Contains(indice.DateUpdated.AddDays(1)))
                {
                    // 🔹 Mettre à jour l'historique de l'indice
                    Console.WriteLine($"Mise à jour de l'historique pour {indice.Name}...");
                    await UpdateHistorique(indice, nomBourse, stoppingToken);

                }

                // ✅ Une fois terminé, on sort de la boucle
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur pour l'indice {indice.Name} : {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    //private async Task GérerIndice(Indice indice, string nomBourse, BourseContext dbContext, IPage page, FuseHoraire fuseHoraire, CancellationToken stoppingToken)
    //{
    //    while (!stoppingToken.IsCancellationRequested)
    //    {
    //        //Vérifier si le marché est toujours ouvert pendant l'exécution de la tâche
    //        if (!EstDansLesHorairesBourse(nomBourse.ToLower()))
    //        {
    //            Console.WriteLine($"Marché fermé, arrêt des tâches pour {indice.Name}");

    //            // Vérifier si minuit n'est pas encore passé
    //            if (DateTime.Now >= DateTime.Today.Add(fuseHoraire.Fermeture) && DateTime.Now < DateTime.Today.AddDays(1))
    //            {
    //                // Appeler la méthode pour attendre la fin de la journée
    //                await WaitForEndOfDay(stoppingToken, fuseHoraire);
    //            }

    //            // Faire la mise à jour des dates financieres
    //            //await UpdateEarningDates(nomBourse, stoppingToken);

    //            // Faire la sauvegarde de la journée
    //            await SauvegarderHistoriqueManquant(nomBourse, fuseHoraire, stoppingToken);

    //            // Atendre l'houverture des marchès
    //            await WaitUntilMarketOpens(stoppingToken, fuseHoraire);

    //        }

    //        try
    //        {
    //            await _semaphore.WaitAsync(stoppingToken);

    //            // Mettre à jour les prediction selon l'historique exhistente
    //            await UpdateHistorique(indice, dbContext, page, nomBourse, stoppingToken);
    //            break;
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Erreur pour l'indice {indice.Name} : {ex.Message}");
    //        }
    //        finally
    //        {
    //            _semaphore.Release();
    //        }
    //    }
    //}

    #region utilities

    private async Task WaitUntilMarketOpens(CancellationToken stoppingToken, FuseHoraire fuseHoraire)
    {
        // Obtenir l'heure actuelle dans le fuseau horaire de la bourse
        DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

        // Déterminer l'heure d'ouverture aujourd'hui
        DateTime marketOpenTime = now.Date.Add(fuseHoraire.Ouverture);

        // Si l'heure actuelle est déjà après l'ouverture ou un week-end, calculer le prochain jour ouvré
        while (now > marketOpenTime || marketOpenTime.DayOfWeek == DayOfWeek.Saturday
                                    || marketOpenTime.DayOfWeek == DayOfWeek.Sunday
                                    || fuseHoraire.JoursFeries.Any(holiday => holiday.Date == marketOpenTime.Date))
        {
            // Ajouter un jour
            marketOpenTime = marketOpenTime.AddDays(1);

            // Sauter le week-end
            if (marketOpenTime.DayOfWeek == DayOfWeek.Saturday)
                marketOpenTime = marketOpenTime.AddDays(2); // Passer au lundi
            else if (marketOpenTime.DayOfWeek == DayOfWeek.Sunday)
                marketOpenTime = marketOpenTime.AddDays(1); // Passer au lundi
        }

        // Calculer le temps restant avant l'ouverture
        TimeSpan timeUntilMarketOpen = marketOpenTime - now;

        // Attendre jusqu'à l'ouverture ou annuler si nécessaire
        try
        {
            await Task.Delay(timeUntilMarketOpen, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Attente de l'ouverture annulée.");
        }
    }

    static async Task WaitForEndOfDay(CancellationToken cancellationToken, FuseHoraire fuseHoraire)
    {
        // Obtenir l'heure actuelle
        DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

        // Calculer l'heure de fin de la journée (minuit)
        DateTime endOfDay = now.Date.AddDays(1);

        if (now < endOfDay)
        {
            TimeSpan timeUntilEndOfDay = endOfDay - now;

            Console.WriteLine($"Temps restant avant la fin de la journée : {timeUntilEndOfDay}");

            // Vérifier si le temps restant est valide (positif)
            if (timeUntilEndOfDay > TimeSpan.Zero)
            {
                try
                {
                    // Attendre la fin de la journée ou annuler si nécessaire
                    await Task.Delay(timeUntilEndOfDay, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Attente annulée avant la fin de la journée.");
                    return;
                }
            }

        }
        else
        {
            Console.WriteLine("L'heure actuelle est déjà passée 24h, pas d'attente nécessaire.");
        }
        // Calculer le temps restant jusqu'à la fin de la journée

        Console.WriteLine("La journée est terminée.");
    }

    private async Task<IEnumerable<Indice>> ObtenirSymbols(string bourse)
    {
        List<Indice> result = new List<Indice>();
        using (var reader = new StreamReader(bourse))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    string[] parts = line.Split('\t');
                    result.Add(new Indice
                    {
                        Symbol = parts[0],
                        Name = parts[1],
                        Bourse = Path.GetFileNameWithoutExtension(bourse)
                    });
                }
            }
        }
        return result;
    }

    // Méthode utilitaire pour gérer les chaînes vides ou nulles
    private float ParseFloatOrDefault(string input, float defaultValue = 0f)
    {
        if (string.IsNullOrWhiteSpace(input))
            return defaultValue;

        if (float.TryParse(input.Replace(" ", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
            return result;

        Console.WriteLine($"⚠️ Impossible de convertir '{input}' en float. Valeur par défaut utilisée : {defaultValue}");
        return defaultValue;
    }

    static DateTime? GetClosestDateToToday(IEnumerable<string> filePaths)
    {
        try
        {
            // Extraire les dates valides à partir des noms de fichiers
            var validDates = filePaths
                .Select(file => ExtractDateFromFileName(file)) // Extraire la date
                .Where(date => date.HasValue) // Garder uniquement les dates valides
                .Select(date => date.Value) // Retirer le Nullable<DateTime>
                .ToList();

            if (!validDates.Any())
            {
                return null; // Retourner null si aucune date valide
            }

            // Trouver la date la plus proche d'aujourd'hui
            DateTime today = DateTime.Today;
            return validDates
                .OrderBy(date => Math.Abs((date - today).Days)) // Trier par proximité
                .First(); // Prendre la date la plus proche
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
            return null; // Retourner null en cas d'erreur
        }
    }

    static DateTime? ExtractDateFromFileName(string filePath)
    {
        try
        {
            // Exemple : TSX_20241122.txt => Extraire "20241122"
            string fileName = Path.GetFileNameWithoutExtension(filePath); // Nom sans extension
            string datePart = fileName.Split('_')[1]; // Extraire la partie après "_"

            // Convertir la chaîne en DateTime
            if (DateTime.TryParseExact(datePart, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                return date;
            }
            else
            {
                return null; // Retourner null si le format est invalide
            }
        }
        catch
        {
            return null; // Retourner null en cas d'erreur
        }
    }

    //static bool HasProperty(IReadOnlyDictionary<string, dynamic> obj, string propertyName)
    //{
    //    return obj != null && obj.ContainsKey(propertyName);

    //    //if (obj == null)
    //    //    return false;
    //    //// Utiliser la réflexion pour obtenir les informations sur la propriété
    //    //Type type = obj.GetType();
    //    //PropertyInfo property = type.GetProperty("key");

    //    //bool hasProperty = obj.ContainsKey(propertyName);
    //    //return hasProperty;
    //}
    #endregion#

    #region updateHistory

    private async Task UpdateHistorique(Indice indice, string nomBourse, CancellationToken stoppingToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            BourseContext dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();

            if (indice == null || dbContext == null)
            {
                Console.WriteLine($"Erreur : Indice ou contexte de base de données nul pour {nomBourse}");
                return;
            }

            // Charger les horaires de la bourse
            FuseHoraire fuseHoraire = GetHoraireOverture(indice.Bourse.ToLower());

            // Convertir la date donnée dans le fuseau horaire de la bourse
            DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

            try
            {
                float[] closePrices = dbContext.StockDatas
                    .Where(i => i.IndiceId == indice.Id)
                    .Select(d => d.PrevPrice)
                    .ToArray();

                // Spécifiez le chemin du répertoire contenant les fichiers .txt pour recuperer les symboles
                string directoryPath = @$"Data/{nomBourse}"; // Remplacez par votre répertoire

                // Verifier si il y a dejà l'historique dans la DB pour l'indice
                if (!dbContext.StockDatas.Any(s => s.IndiceId == indice.Id))
                {
                    Console.WriteLine($"Aucun historique trouvé pour {indice.Name}, récupération des données...");

                    try
                    {
                        // Récupère tous les fichiers .txt du répertoire hystorique
                        List<string> txtFiles = Directory.GetFiles(directoryPath, "*.txt")
                            .ToList();

                        if (!string.IsNullOrEmpty(indice.Symbol))
                        {
                            // Ajout les historiques manquantes dans StockDatas
                            indice.TrainingData = AjoutStockData(txtFiles, indice.Symbol, closePrices);
                        }

                    }
                    catch (DirectoryNotFoundException e)
                    {
                        Console.WriteLine($"Erreur : Le répertoire spécifié n'existe pas. {e.Message}");
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine($"Erreur : Accès refusé au répertoire. {e.Message}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Une erreur s'est produite : {e.Message}");
                    }
                }
                else
                {
                    // 🔹 Calculs des indicateurs financiers

                    //if (closePrices.Length > 0)
                    //{
                    //    float[] sma14 = CalculateSMA(closePrices, 14);
                    //    float[] rsi14 = CalculateRSI(closePrices, 14);

                    //    for (int i = 0; i < indice.TrainingData.Count; i++)
                    //    {
                    //        indice.TrainingData[i].RSI_14 = rsi14[i] != null ? rsi14[i] : 0;
                    //        indice.TrainingData[i].SMA_14 = sma14[i] != null ? sma14[i] : indice.TrainingData[i].CurrentPrice;
                    //    }
                    //}

                    // 🔹 Vérifier s'il existe des fichiers plus récents

                    DateTime? lastDateInStockDatas = dbContext.StockDatas
                        .Where(i => i.IndiceId == indice.Id)
                        .OrderByDescending(i => i.Date)
                        .Select(i => i.Date)
                        .FirstOrDefault();

                    // Gérer le cas où aucune donnée n'existe dans StockDatas
                    lastDateInStockDatas ??= dateDansLeFuseau.AddDays(-2);

                    try
                    {

                        // Trouver les fichiers avec des dates supérieures à la dernière date
                        List<string> newFiles = Directory.GetFiles(directoryPath, "*.txt")
                            .Where(file =>
                            {

                                string fileName = Path.GetFileNameWithoutExtension(file); // Nom du fichier sans extension
                                int underscoreIndex = fileName.LastIndexOf('_'); // Recherche du dernier underscore

                                if (underscoreIndex != -1 && underscoreIndex < fileName.Length - 1)
                                {
                                    string potentialDate = fileName[(underscoreIndex + 1)..];

                                    // Vérification si la chaîne extraite est une date valide (format "yyyyMMdd")
                                    if (DateTime.TryParseExact(potentialDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fileDate))
                                    {
                                        return fileDate > lastDateInStockDatas; // Garder les fichiers dont la date est supérieure
                                    }
                                }
                                return false; // Ignorer les fichiers mal formés

                            })
                            .OrderBy(file => file) // Optionnel : Trier par nom de fichier
                            .ToList();

                        if (!string.IsNullOrEmpty(indice.Symbol) && newFiles.Count > 0)
                        {
                            // Ajout les historiques manquantes dans StockDatas
                            indice.TrainingData.AddRange(AjoutStockData(newFiles, indice.Symbol, closePrices));
                        }

                    }
                    catch (DirectoryNotFoundException e)
                    {
                        Console.WriteLine($"Erreur : Le répertoire spécifié n'existe pas. {e.Message}");
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine($"Erreur : Accès refusé au répertoire. {e.Message}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Une erreur s'est produite : {e.Message}");
                    }

                }

                if (indice.DateUpdated != dateDansLeFuseau)
                {
                    decimal rsi = 50;
                    string date = DateOnly.FromDateTime(dateDansLeFuseau).ToString();

                    // Si des données d'entraînement existent, utiliser les données les plus récentes
                    if (indice.TrainingData?.Count > 0)
                    {
                        var latestTrainingData = indice.TrainingData.OrderByDescending(t => t.Date).FirstOrDefault();
                        if (latestTrainingData != null)
                        {
                            if (latestTrainingData?.RSI_14 != null &&
                                !float.IsNaN(latestTrainingData.RSI_14) &&
                                !float.IsInfinity(latestTrainingData.RSI_14))
                            {
                                try
                                {
                                    rsi = Convert.ToDecimal(latestTrainingData.RSI_14);
                                }
                                catch (OverflowException)
                                {
                                    Console.WriteLine($"⚠️ RSI_14 dépasse les limites du Decimal : {latestTrainingData.RSI_14}");
                                    rsi = 50; // Valeur par défaut en cas d'erreur
                                }
                            }
                            else
                            {
                                Console.WriteLine("⚠️ RSI_14 est invalide ou null, valeur par défaut utilisée.");
                            }

                            date = DateOnly.FromDateTime(latestTrainingData.Date).ToString();
                        }
                        indice.RegularMarketPrice = latestTrainingData.CurrentPrice;
                        indice.RegularMarketPreviousClose = latestTrainingData.PrevPrice;
                        indice.RegularMarketOpen = latestTrainingData.Open;
                        indice.RegularMarketDayLow = latestTrainingData.Low;
                        indice.RegularMarketDayHigh = latestTrainingData.High;
                        indice.RegularMarketChange = latestTrainingData.Change;
                        indice.RegularMarketChangePercent = latestTrainingData.ChangePercent;
                    }

                    // Générer une recommandation basée sur RSI
                    string recommendation = await GetRecommendationBasedOnRSI(rsi);

                    // Mise à jour de l'indice dans la base de données
                    indice.DateUpdated = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    indice.Raccomandation = recommendation;
                }

                if (indice.DatePrevision != DateOnly.FromDateTime(dateDansLeFuseau))
                {
                    StockPrediction? prediction = Prediction(indice);

                    if (prediction != null)
                    {
                        indice.IsIncreasing = prediction.IsIncreasing;

                        if (float.IsNaN(prediction.Probability) || float.IsInfinity(prediction.Probability))
                        {
                            Console.WriteLine($"Correction de Probability NaN/Infini pour l'indice {indice.Id}");
                            prediction.Probability = 0; // Mets une valeur par défaut ou un calcul de secours
                        }
                        indice.Probability = prediction.Probability;
                    }
                    indice.DatePrevision = DateOnly.FromDateTime(dateDansLeFuseau);
                }

                // 🔹 Sauvegarde des modifications dans la DB avec gestion des verrous

                // Definitions des parametres de mise à jour du DB
                int maxRetries = 5;
                int delay = 1000; // in milliseconds


                for (int retry = 0; retry < maxRetries; retry++)
                {
                    try
                    {
                        // Vérifie si l'indice a un ID valide
                        if (indice.Id == 0)
                        {
                            Console.WriteLine("Erreur : Indice.Id est 0, impossible de l'ajouter !");
                            break;
                        }

                        var existingIndice = await dbContext.Indices
                            .Include(i => i.TrainingData) // Charge la liste associée
                            .FirstOrDefaultAsync(i => i.Id == indice.Id);

                        if (existingIndice == null)
                        {
                            Console.WriteLine($"Ajout de l'indice {indice.Symbol} (ID: {indice.Id}) à la base.");
                            await dbContext.Indices.AddAsync(indice);
                        }
                        else
                        {
                            Console.WriteLine($"Mise à jour de l'indice {indice.Symbol} (ID: {indice.Id}).");
                            dbContext.Entry(existingIndice).CurrentValues.SetValues(indice);

                            // Vérifier si `trainingData` doit être mis à jour
                            if (indice.TrainingData != null)
                            {
                                // Suppression des anciennes valeurs si nécessaire
                                existingIndice.TrainingData.Clear();

                                // Ajout des nouvelles données
                                existingIndice.TrainingData.AddRange(indice.TrainingData);
                            }
                        }

                        await dbContext.SaveChangesAsync();
                        return; // Succès, on sort de la boucle
                    }
                    catch (DbUpdateException ex)
                    {
                        Console.WriteLine($"Erreur DB: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Détails internes: {ex.InnerException.Message}");
                        }
                        await Task.Delay(delay);
                        delay *= 2; // Augmente le temps d'attente en cas de problème
                    }
                    catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // Error 5 is database lock
                    {
                        Console.WriteLine("Base de données verrouillée, nouvel essai...");
                        await Task.Delay(delay); // Wait before retrying
                        delay *= 2; // Optionally, use exponential backoff
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur s'est produite : {ex.Message}");
                Console.WriteLine($"Error querying Yahoo Finance API for symbol: {indice.Symbol}");
            }
        }
    }

    private List<StockData> AjoutStockData(List<string> files, string symbol, float[] closePrices)
    {
        List<Price> prices = new List<Price>();

        // Parcourt et lit chaque fichier .txt
        foreach (string file in files)
        {
            bool isFirstLine = true; // Variable pour contrôler la première ligne

            // Lire chaque ligne du fichier
            foreach (string line in File.ReadLines(file))
            {
                // Ignore la première ligne
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue; // Passe à la ligne suivante
                }

                string[] parts = line.Split(',');

                if (parts.Length < 6) // Vérification
                    continue;

                //// Supprimer ".TO" si présent
                //symbol = symbol.EndsWith(".TO") ? symbol.Substring(0, symbol.Length - 3) : symbol;

                if (parts[0] == symbol)
                {
                    Price price = new Price
                    {
                        Date = parts[1],
                        Open = ParseFloatOrDefault(parts[2]),
                        High = ParseFloatOrDefault(parts[3]),
                        Low = ParseFloatOrDefault(parts[4]),
                        Close = ParseFloatOrDefault(parts[5])
                    };
                    prices.Add(price);
                }
            }
        }
        //Ajout de la liste de l'historique de chaque indices
        var trainingData = new List<StockData>();

        if (prices.Count != 0)
        {
            float[] sma14 = Array.Empty<float>();
            float[] rsi14 = Array.Empty<float>();

            if (closePrices.Length > 14)
            {
                // Mettre à l'interieur du if et supprimer l'exception
                sma14 = CalculateSMA(closePrices, 14);
                rsi14 = CalculateRSI(closePrices, 14);
            }


            for (int i = 0; i < prices.Count; i++)
            {
                var currentPrice = prices[i].Close;
                var prevPrice = i == 0 ? prices[i].Close : prices[i - 1].Close;

                // Sécuriser l'accès aux indices SMA et RSI
                //int smaIndex = Math.Max(0, Math.Min(sma14.Length - 1, sma14.Length - (prices.Count - i)));
                //int rsiIndex = Math.Max(0, Math.Min(rsi14.Length - 1, rsi14.Length - (prices.Count - i)));

                //int smaIndex = Math.Max(0, Math.Min(sma14.Length - 1, i));
                //int rsiIndex = Math.Max(0, Math.Min(rsi14.Length - 1, i));

                int smaIndex = Math.Max((sma14.Length - prices.Count) + i, i);
                int rsiIndex = Math.Max((rsi14.Length - prices.Count) + i, i);

                float smaValue = sma14.Length > 0 ? sma14[smaIndex] : 0;
                float rsiValue = rsi14.Length > 0 ? rsi14[rsiIndex] : 0;

                // Créer une instance de StockData
                var stockData = new StockData
                {
                    CurrentPrice = currentPrice,
                    Open = prices[i].Open,
                    High = prices[i].High,
                    Low = prices[i].Low,
                    SMA_14 = smaValue,
                    RSI_14 = rsiValue,
                    Date = DateTime.ParseExact(prices[i].Date, "yyyyMMdd", null),
                    PrevPrice = prevPrice
                };

                trainingData.Add(stockData);
            }
        }
        else
        {
            Console.WriteLine("Aucune donnée de prix chargée.");
        }

        return trainingData;
    }

    public StockPrediction? Prediction(Indice indice)
    {

        // Vérifier s'il y a assez de données pour l'entraînement
        if (indice.TrainingData == null || !indice.TrainingData.Any())
        {
            Console.WriteLine("Données d'entraînement insuffisantes.");
            return null;
        }

        // Convertir la liste de données en IDataView
        var trainingDataView = _mlContext.Data.LoadFromEnumerable(indice.TrainingData);

        // Définir le pipeline d'apprentissage (Classification)
        var pipeline = _mlContext.Transforms.Concatenate("Features", nameof(StockData.CurrentPrice), nameof(StockData.Open), nameof(StockData.High), nameof(StockData.Low), nameof(StockData.RSI_14), nameof(StockData.SMA_14))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(StockData.IsIncreasing),
                featureColumnName: "Features",
                maximumNumberOfIterations: 100));

        // Entraîner le modèle
        var model = pipeline.Fit(trainingDataView);

        // Dernière donnée pour la prédiction
        var lastData = indice.TrainingData.OrderByDescending(t => t.Id).FirstOrDefault();
        if (lastData == null) return null;

        // Créer un échantillon avec toutes les caractéristiques
        var sampleData = new StockData
        {
            CurrentPrice = (float)indice.RegularMarketPrice,
            Open = (float)indice.RegularMarketOpen,
            High = (float)indice.RegularMarketDayHigh,
            Low = (float)indice.RegularMarketDayLow,
            RSI_14 = lastData.RSI_14,
            SMA_14 = lastData.SMA_14
        };

        // Faire des prédictions
        var predictionFunction = _mlContext.Model.CreatePredictionEngine<StockData, StockPrediction>(model);

        // Faire une prédiction sur l'échantillon
        var prediction = predictionFunction.Predict(sampleData);

        Console.WriteLine($"La prédiction indique que le prix va {(prediction.IsIncreasing ? "augmenter" : "diminuer")}.");

        return prediction;
    }

    //private async Task UpdateEarningDates(string nomBourse, CancellationToken stoppingToken)
    //{
    //    using (var scope = _serviceProvider.CreateScope())
    //    {
    //        var dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();
    //        List<Indice> indices = await dbContext.Indices.Include(i => i.TrainingData).Where(i => i.Bourse == nomBourse).ToListAsync();

    //        //Récupérer la liste des entreprises dont la date des états financières est proche
    //        var companyEarningsDates = await GetAllEarningsInfoAsync();

    //        foreach (var indice in indices)
    //        {
    //            List<DateTime> earningsDates = new();

    //            //EarningsInfo? earningsInfo = companyEarningsDates.FirstOrDefault(c => c.Symbol == indice.Symbol);

    //            EarningsInfo? earningsInfo = companyEarningsDates.FirstOrDefault(c => c.Symbol == indice.Symbol);

    //            if (earningsInfo != null) 
    //            {
    //                indice.EarningsInfo = earningsInfo;
    //                //earningsDates.Add(earningsInfo.Date);
    //            }

    //            //var earningsDates = await GetFinancialDataYahooAsync(indice, dbContext);

    //            //if (earningsDates != null && earningsDates.Count > 0)
    //            //{
    //            //    indice.DatesExercicesFinancieres = earningsDates.ToArray();

    //            //}
    //            //else
    //            //{
    //            //    indice.DatesExercicesFinancieres = new DateTime[] { new DateTime() };
    //            //    Console.WriteLine($"Aucune donnée trouvée pour le symbole {indice.Symbol}.");
    //            //}

    //            dbContext.Indices.Update(indice);
    //        }
    //        dbContext.SaveChanges();
    //    }
    //}

    //private async Task<List<EarningsInfo>> GetAllEarningsInfoAsync()
    //{
    //    int currentPage = 1;
    //    bool morePages = true;
    //    var allEarningsInfo = new List<EarningsInfo>();

    //    while (morePages)
    //    {
    //        // Récupérer les informations de la page courante
    //        var earningsInfoForPage = await GetEarningsFromZoneBourse(currentPage);

    //        // Si aucune information n'est trouvée, arrêter
    //        if (earningsInfoForPage.Count == 0)
    //        {
    //            morePages = false;
    //        }
    //        else
    //        {
    //            allEarningsInfo.AddRange(earningsInfoForPage);
    //            currentPage++;
    //        }
    //    }

    //    return allEarningsInfo;
    //}

    #endregion#

    #region saveHistory
    private async Task SauvegarderHistoriqueManquant(string nomBourse, FuseHoraire fuseHoraire, CancellationToken stoppingToken)
    {
        DateTime dateLastClose = await GetLastDateHistory(nomBourse);
        DateTime dateNext = dateLastClose.AddDays(1);

        // Convertir la date donnée dans le fuseau horaire de la bourse
        DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

        // Vérifier que le jour à sauvegrader est un jour boursiere et inferieur à aujourd'hui
        while (dateNext.Date < dateDansLeFuseau.Date)
        {

            // Ignore les week-ends
            if (dateNext.DayOfWeek != DayOfWeek.Saturday
                && dateNext.DayOfWeek != DayOfWeek.Sunday
                && !fuseHoraire.JoursFeries.Any(holiday => holiday.Date == dateNext.Date))
            {
                string filePath = $"Data/{nomBourse}/{nomBourse}_{dateNext:yyyyMMdd}.txt";

                try
                {
                    // Sauvegarde l'historique pour le jour suivant
                    await SaveHistory(filePath, nomBourse, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la sauvegarde pour {dateNext:yyyy-MM-dd}: {ex.Message}");
                }
            }

            // Passe au jour suivant
            dateNext = dateNext.AddDays(1);
        }
    }

    private async Task SaveHistory(string filePath, string nomBourse, CancellationToken stoppingToken)
    {
        // ---- NOTES ----
        // AJOUTER ALPHA VANATAGE IMPLEMENTATION POUR LA SAUVEGARDE JOURNALIER.
        // SUPPRIMER ZONEBOURSE IMPLEMENTATION ET LA GARDER JUSTE POUR LES DATES DES ETATS FINANCIERES

        using (var scope = _scopeFactory.CreateScope())
        {
            BourseContext dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();

            List<Indice> indices = await dbContext.Indices.Include(i => i.TrainingData).Where(i => i.Bourse == nomBourse).ToListAsync();

            string header = "<ticker>,<date>,<open>,<high>,<low>,<close>,<vol>";

            using (StreamWriter sw = File.CreateText(filePath))
            {
                // Écrire l'en-tête
                sw.WriteLine(header);

                // Boucler sur chaque liste interne et écrire les propriétés séparées par des virgules
                foreach (var indice in indices)
                {
                    // Charger les horaires de la bourse
                    FuseHoraire fuseHoraire = GetHoraireOverture(indice.Bourse.ToLower());

                    // Convertir la date donnée dans le fuseau horaire de la bourse
                    DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

                    // Zonebourse WebScraping implementation

                    //string? name = string.Join(" ", indice.Name.Split(' ').Take(2));

                    //string? name = indice.Name;
                    //if (name != null)
                    //{
                    //    // Liste des mots à ignorer
                    //    var motsASupprimer = new HashSet<string> { "Inc", "The" };

                    //    // Séparer le nom en mots
                    //    var words = name.Split(' ');

                    //    // Trouver le premier mot valide (qui n'est pas "Inc" ou "The")
                    //    name = words.FirstOrDefault(word => !motsASupprimer.Contains(word)) ?? string.Empty;
                    //}

                    string? name = indice.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        // Liste des mots à ignorer
                        var motsASupprimer = new HashSet<string> { "Inc", "The" };

                        // Séparer le nom en mots valides (on ignore les mots interdits)
                        var motsValides = name
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(word => !motsASupprimer.Contains(word))
                            .ToList();

                        // Construire le nouveau nom
                        StringBuilder newName = new();
                        int totalLength = 0;

                        foreach (var word in motsValides)
                        {
                            if (totalLength >= 3) break; // Stop si on a atteint 3 caractères

                            if (newName.Length > 0) newName.Append(" "); // Ajouter un espace si nécessaire

                            newName.Append(word);
                            totalLength = newName.ToString().Replace(" ", "").Length; // Calculer sans espace
                        }

                        name = newName.ToString();
                    }

                    Console.WriteLine(name);


                    string? symbolCut = indice.Symbol?.EndsWith(".TO") == true ? indice.Symbol[..^3] : indice.Symbol;
                    string? bourse;

                    switch (indice.Bourse)
                    {
                        case "TSX":
                            bourse = "Toronto";
                            break;
                        case "NASDAQ":
                            bourse = "Nasdaq";
                            break;
                        case "AMEX":
                            bourse = "Nyse";
                            break;
                        case "NYSE":
                            bourse = "Nyse";
                            break;
                        default:
                            bourse = indice.Bourse;
                            break;
                    }

                    string regularMarketTime = dateDansLeFuseau.AddDays(-1).ToString("yyyyMMdd");

                    string line;

                    try
                    {
                        string searchUrl;
                        string urlCompanyName = string.Empty;

                        if (symbolCut.Length > 2)
                        {
                            searchUrl = $"https://www.zonebourse.com/recherche/instruments?q={symbolCut}";
                            urlCompanyName = await GetCompanyName(searchUrl, symbolCut, bourse);
                        }
                        else
                        {
                            searchUrl = $"https://www.zonebourse.com/recherche/instruments?q={name}";
                            urlCompanyName = await GetCompanyName(searchUrl, symbolCut, string.Empty);
                        }

                        if (string.IsNullOrEmpty(urlCompanyName))
                        {
                            searchUrl = $"https://www.zonebourse.com/recherche/instruments?q={name}";
                            urlCompanyName = await GetCompanyName(searchUrl, symbolCut, string.Empty);
                        }

                        if (string.IsNullOrEmpty(urlCompanyName))
                        {
                            name = indice.Name;
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                // Liste des mots à ignorer
                                var motsASupprimer = new HashSet<string> { "Inc", "The" };

                                // Séparer le nom en mots et filtrer ceux à ignorer
                                var motsFiltrés = name
                                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                    .Where(word => !motsASupprimer.Contains(word));

                                // Reconstruire le nom avec les mots valides
                                name = string.Join(" ", motsFiltrés);
                            }

                            searchUrl = $"https://www.zonebourse.com/recherche/instruments?q={name}";
                            urlCompanyName = await GetCompanyName(searchUrl, symbolCut, string.Empty);
                        }

                        if (!string.IsNullOrEmpty(urlCompanyName))
                        {
                            string url = $"https://www.zonebourse.com{urlCompanyName}cotations/";

                            using HttpClient client = new HttpClient();
                            var response = await client.GetStringAsync(url);

                            var htmlDoc = new HtmlDocument();
                            htmlDoc.LoadHtml(response);

                            // Méthode pour récupérer la dernière valeur d'une ligne spécifique
                            string? GetLastColumnValue(string rowLabel)
                            {
                                var xpath = $"//table[@id='cotation-5days-table']//tbody//tr[td[1][normalize-space()='{rowLabel}']]//td[last()]";
                                var node = htmlDoc.DocumentNode.SelectSingleNode(xpath);
                                return node?.InnerText.Trim().Replace("\u00A0", "").Replace("$", "").Replace(",", ".").Replace(" ", "");
                            }

                            string? ouverture = !String.IsNullOrEmpty(GetLastColumnValue("Ouverture")) ? GetLastColumnValue("Ouverture") : "0";
                            string? high = GetLastColumnValue("Plus haut");
                            string? low = GetLastColumnValue("Plus bas");
                            string? close = GetLastColumnValue("Dernier");
                            string? volume = GetLastColumnValue("Volume");

                            line = string.Join(",",
                                indice.Symbol,
                                regularMarketTime,
                                ouverture,
                                high,
                                low,
                                close,
                                volume);

                            sw.WriteLine(line); // Écrire la ligne dans le fichier
                        }
                        else
                        {
                            Console.WriteLine("Indice non trouvé.");
                        }

                        //FlurlHttp.Configure(settings =>
                        //{
                        //    settings.Timeout = TimeSpan.FromSeconds(60); // Increase timeout to 60 seconds
                        //});

                        //var securities = await Yahoo.Symbols(symbol).Fields(
                        //    Field.RegularMarketChange,
                        //    Field.RegularMarketChangePercent,
                        //    Field.RegularMarketDayHigh,
                        //    Field.RegularMarketDayLow,
                        //    Field.RegularMarketOpen,
                        //    Field.RegularMarketPreviousClose,
                        //    Field.RegularMarketPrice,
                        //    Field.RegularMarketVolume
                        //).QueryAsync();

                        //if (securities.ContainsKey(symbol))
                        //{
                        //    var data = securities[symbol];

                        //    if (data.Fields != null)
                        //    {
                        //        try
                        //        {
                        //            regularMarketTime = DateTimeOffset.FromUnixTimeSeconds(data[Field.RegularMarketTime]).ToString("yyyyMMdd");
                        //        }
                        //        catch (KeyNotFoundException)
                        //        {
                        //            regularMarketTime = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
                        //        }

                        //        line = string.Join(",",
                        //            symbol,
                        //            regularMarketTime,
                        //            GetFieldValueSafely(data, indice, Field.RegularMarketOpen),
                        //            GetFieldValueSafely(data, indice, Field.RegularMarketDayHigh),
                        //            GetFieldValueSafely(data, indice, Field.RegularMarketDayLow),
                        //            GetFieldValueSafely(data, indice, Field.RegularMarketPrice),
                        //            GetFieldValueSafely(data, indice, Field.RegularMarketVolume));
                        //    }
                        //}
                        //else
                        //{
                        //    Console.WriteLine($"Symbol '{symbol}' not found in the response.");
                        //}

                        //await Task.Delay(1000);

                    }
                    catch (FlurlHttpException httpEx)
                    {
                        //Console.WriteLine($"Erreur HTTP pour {symbol} : {httpEx.StatusCode} - {httpEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur pour {indice.Symbol} : {ex.Message}");
                    }



                    //    try
                    //{
                    //    // Récupérer les données historiques (exemple : 30 derniers jours)
                    //    var history = await Yahoo.GetHistoricalAsync(symbol, DateTime.Now.AddDays(-30), DateTime.Now, Period.Daily);

                    //    foreach (var dataPoint in history)
                    //    {
                    //        string line = string.Join(",",
                    //            symbol,
                    //            dataPoint.DateTime.ToString("yyyyMMdd"),
                    //            dataPoint.Open,
                    //            dataPoint.High,
                    //            dataPoint.Low,
                    //            dataPoint.Close,
                    //            dataPoint.Volume);

                    //        sw.WriteLine(line); // Écrire la ligne dans le fichier

                    //        // Affichage console (optionnel)
                    //        Console.WriteLine($"Symbol: {symbol}");
                    //        Console.WriteLine($"Date: {dataPoint.DateTime.ToString("yyyy-MM-dd")}");
                    //        Console.WriteLine($"Open: {dataPoint.Open}");
                    //        Console.WriteLine($"High: {dataPoint.High}");
                    //        Console.WriteLine($"Low: {dataPoint.Low}");
                    //        Console.WriteLine($"Close: {dataPoint.Close}");
                    //        Console.WriteLine($"Volume: {dataPoint.Volume}");
                    //        Console.WriteLine();

                    //        await Task.Delay(1000);
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine($"Erreur pour {symbol} : {ex.Message}");
                    //}


                    //// Alpha Vantage Api implementation
                    //string symbol = indice.Symbol; // Symbole boursier (ex. : Apple)
                    //string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&entitlement=delayed&outputsize=full&&interval=5min&apikey={_apiKey}";

                    //using (WebClient client = new WebClient())
                    //{
                    //    try
                    //    {
                    //        dynamic json_data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(url));

                    //        // Affiche les métadonnées
                    //        if (json_data.ContainsKey("Meta Data"))
                    //        {
                    //            Console.WriteLine("=== Meta Data ===");
                    //            var metaData = json_data["Meta Data"];

                    //            foreach (var entry in metaData.EnumerateObject())
                    //            {
                    //                string date = entry.Name; // Nom de la propriété (la date)
                    //                string values = entry.Value.ToString();
                    //                Console.WriteLine($"{entry.Name}: {entry.Value.GetString()}");
                    //                Console.WriteLine();
                    //            }
                    //        }

                    //        // Accéder aux séries temporelles
                    //        if (json_data.ContainsKey("Time Series (Daily)"))
                    //        {
                    //            Console.WriteLine("\n=== Time Series (Daily) ===");
                    //            var timeSeries = json_data["Time Series (Daily)"];

                    //            foreach (var entry in timeSeries.EnumerateObject())
                    //            {
                    //                string datePart = ExtractDateFromFilePath(filePath);
                    //                string dateJson = entry.Name; // Date
                    //                var values = entry.Value; // Valeurs associées

                    //                if (dateJson.Replace("-", "") == datePart)
                    //                {
                    //                    // Extraire la date du nom de fichier

                    //                    string dateHistorique;

                    //                    if (DateTime.TryParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    //                    {
                    //                        dateHistorique = date.ToString("yyyyMMdd");
                    //                    }
                    //                    else
                    //                    {
                    //                        dateHistorique = DateTime.Now.ToString("yyyyMMdd");
                    //                        Console.WriteLine("Le fichier ne contient pas une date valide.");
                    //                    }

                    //                    string line = string.Join(",", indice.Symbol, dateHistorique,
                    //                    values.GetProperty("1. open").GetString(),
                    //                    values.GetProperty("2. high").GetString(),
                    //                    values.GetProperty("3. low").GetString(),
                    //                    values.GetProperty("4. close").GetString(),
                    //                    values.GetProperty("5. volume").GetString());

                    //                    sw.WriteLine(line); // Écrire la ligne dans le fichier

                    //                    Console.WriteLine($"Date: {date}");
                    //                    Console.WriteLine($"Open: {values.GetProperty("1. open").GetString()}");
                    //                    Console.WriteLine($"High: {values.GetProperty("2. high").GetString()}");
                    //                    Console.WriteLine($"Low: {values.GetProperty("3. low").GetString()}");
                    //                    Console.WriteLine($"Close: {values.GetProperty("4. close").GetString()}");
                    //                    Console.WriteLine($"Volume: {values.GetProperty("5. volume").GetString()}");
                    //                    Console.WriteLine();

                    //                    //await Task.Delay(TimeSpan.FromMilliseconds(400));
                    //                    continue;
                    //                }

                    //            }
                    //        }

                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine($"Erreur : {ex.Message}");
                    //    }
                    //}
                }
            }

            //// Boucler sur chaque liste interne et écrire les propriétés séparées par des virgules
            //foreach (var indice in indices)
            //{
            //    DateTime? lastDateInStockDatas = dbContext.StockDatas
            //            .Where(i => i.IndiceId == indice.Id)
            //            .OrderByDescending(i => i.Date)
            //            .Select(i => i.Date)
            //            .FirstOrDefault();

            //    // Gérer le cas où aucune donnée n'existe dans StockDatas
            //    if (!lastDateInStockDatas.HasValue)
            //    {
            //        Console.WriteLine("Aucune donnée dans StockDatas.");
            //        lastDateInStockDatas = DateTime.MinValue; // Considérer une date très ancienne
            //    }

            //    // Spécifiez le chemin du répertoire contenant les fichiers .txt pour recuperer les symboles
            //    string directoryPath = @$"Data/{nomBourse}"; // Remplacez par votre répertoire


            //    try
            //    {
            //        // Récupère tous les fichiers .txt du répertoire hystorique
            //        var files = Directory.GetFiles(directoryPath, "*.txt")
            //            .ToList();

            //        // Trouver les fichiers avec des dates supérieures à la dernière date
            //        var newFiles = files
            //            .Where(file =>
            //            {
            //                // Extraire la date à partir du nom du fichier (format "TSX_YYYYMMDD")
            //                string fileName = Path.GetFileNameWithoutExtension(file); // Nom du fichier sans extension

            //                int underscoreIndex = fileName.LastIndexOf('_'); // Recherche du dernier underscore
            //                if (underscoreIndex != -1 && underscoreIndex < fileName.Length - 1)
            //                {
            //                    string potentialDate = fileName.Substring(underscoreIndex + 1);

            //                    // Vérification si la chaîne extraite est une date valide (format "yyyyMMdd")
            //                    if (DateTime.TryParseExact(potentialDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fileDate))
            //                    {
            //                        return fileDate > lastDateInStockDatas; // Garder les fichiers dont la date est supérieure
            //                    }
            //                }

            //                return false; // Ignorer les fichiers mal formés
            //            })
            //            .OrderBy(file => file) // Optionnel : Trier par nom de fichier
            //            .ToList();

            //        if (indice.Symbol != null)
            //        {
            //            float[] closePrices = dbContext.StockDatas.Where(i => i.IndiceId == indice.Id).Select(d => d.PrevPrice).ToArray();

            //            // Ajout les historiques manquantes dans StockDatas
            //            indice.TrainingData.AddRange(AjoutStockData(newFiles, indice.Symbol, closePrices));
            //        }

            //    }
            //    catch (DirectoryNotFoundException e)
            //    {
            //        Console.WriteLine($"Erreur : Le répertoire spécifié n'existe pas. {e.Message}");
            //    }
            //    catch (UnauthorizedAccessException e)
            //    {
            //        Console.WriteLine($"Erreur : Accès refusé au répertoire. {e.Message}");
            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine($"Une erreur s'est produite : {e.Message}");
            //    }
            //    dbContext.Indices.Update(indice);
            //}
            //dbContext.SaveChanges();
        }
    }

    private async Task<DateTime> GetLastDateHistory(string bourse)
    {
        string nomBourse = Path.GetFileNameWithoutExtension(bourse);

        // Charger les horaires de la bourse
        FuseHoraire fuseHoraire = GetHoraireOverture(nomBourse.ToLower());

        // Convertir la date donnée dans le fuseau horaire de la bourse
        DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

        // Chemin du dossier contenant les fichiers
        string folderPath = $"Data/{bourse}/";

        // Vérifier si le dossier existe
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"Le dossier spécifié n'existe pas : {folderPath}");
            return dateDansLeFuseau;
        }

        // Récupérer tous les fichiers .txt dans le dossier
        var fileNames = Directory.GetFiles(folderPath, "*.txt");

        // Trouver la date la plus proche
        DateTime? closestDate = GetClosestDateToToday(fileNames);

        if (closestDate.HasValue)
        {
            Console.WriteLine($"La date la plus proche d'aujourd'hui est : {closestDate.Value:yyyy-MM-dd}");
            return closestDate.Value;
        }
        else
        {
            Console.WriteLine("Aucune date valide trouvée parmi les fichiers.");
            return dateDansLeFuseau;
        }
    }

    //private string GetDecimalValue(HtmlDocument doc, string xpath)
    //{
    //    var node = doc.DocumentNode.SelectSingleNode(xpath);
    //    return node != null && decimal.TryParse(node.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value)
    //        ? value.ToString()
    //        : "0";
    //}

    //private string GetLongValue(HtmlDocument doc, string xpath)
    //{
    //    var node = doc.DocumentNode.SelectSingleNode(xpath);
    //    return node != null && long.TryParse(node.InnerText.Replace(",", ""), out long value)
    //        ? value.ToString()
    //        : "0";
    //}

    //private static string GetFieldValueSafely(Security data, Indice indice, Field field)
    //{
    //    try
    //    {
    //        return data[field]?.ToString() ?? GetPreviousValue(indice, field);
    //    }
    //    catch (KeyNotFoundException)
    //    {
    //        return GetPreviousValue(indice, field);
    //    }
    //}

    //private static string GetPreviousValue(Indice indice, Field field)
    //{
    //    return field switch
    //    {
    //        Field.RegularMarketPrice => indice.RegularMarketPrice?.ToString() ?? "0",
    //        Field.RegularMarketOpen => indice.RegularMarketOpen?.ToString() ?? "0",
    //        Field.RegularMarketDayHigh => indice.RegularMarketDayHigh?.ToString() ?? "0",
    //        Field.RegularMarketDayLow => indice.RegularMarketDayLow?.ToString() ?? "0",
    //        Field.RegularMarketVolume => indice.RegularMarketVolume?.ToString() ?? "0",
    //        _ => "0"  // Valeur par défaut si le champ n'existe pas dans Indice
    //    };
    //}

    //private string GetTimezoneName(string bourse)
    //{
    //    // Utilisation de noms de bourses en minuscule pour assurer la compatibilité
    //    switch (bourse.ToLower())
    //    {
    //        case "tsx":
    //            return "America/Toronto";

    //        case "nyse":
    //        case "nasdaq":
    //        case "amex":
    //            return "America/New_York";

    //        // Ajoutez d'autres bourses ici si nécessaire
    //        case "lse": // Exemple pour le London Stock Exchange
    //            return "Europe/London";

    //        case "tse": // Exemple pour la Tokyo Stock Exchange
    //            return "Asia/Tokyo";

    //        default:
    //            throw new ArgumentException($"Fuseau horaire non reconnu pour la bourse : {bourse}");
    //    }
    //}

    #endregion

    #region financialData
    private async Task UpdateEarningDates(string nomBourse, CancellationToken stoppingToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            BourseContext dbContext = scope.ServiceProvider.GetRequiredService<BourseContext>();
            List<Indice> indices = await dbContext.Indices.Include(i => i.TrainingData).Where(i => i.Bourse == nomBourse).ToListAsync();

            foreach (var indice in indices)
            {
                string? name = indice.Name;
                if (name != null)
                {
                    // Liste des mots à ignorer
                    var motsASupprimer = new HashSet<string> { "Inc", "The" };

                    // Séparer le nom en mots
                    var words = name.Split(' ');

                    // Trouver le premier mot valide (qui n'est pas "Inc" ou "The")
                    name = words.FirstOrDefault(word => !motsASupprimer.Contains(word)) ?? string.Empty;
                }

                Console.WriteLine(name);


                string? symbolCut = indice.Symbol?.EndsWith(".TO") == true ? indice.Symbol[..^3] : indice.Symbol;
                string? bourse;

                switch (indice.Bourse)
                {
                    case "TSX":
                        bourse = "Toronto";
                        break;
                    case "NASDAQ":
                        bourse = "Nasdaq";
                        break;
                    case "AMEX":
                        bourse = "Nyse";
                        break;
                    case "NYSE":
                        bourse = "Nyse";
                        break;
                    default:
                        bourse = indice.Bourse;
                        break;
                }

                if (symbolCut != null && bourse != null)
                {
                    string searchUrl;
                    string urlCompanyName = string.Empty;

                    if (symbolCut.Length > 2)
                    {
                        searchUrl = $"https://www.zonebourse.com/recherche/instruments?q={symbolCut}";
                        urlCompanyName = await GetCompanyName(searchUrl, symbolCut, bourse);
                    }
                    else
                    {
                        searchUrl = $"https://www.zonebourse.com/recherche/instruments?q={name}";
                        urlCompanyName = await GetCompanyName(searchUrl, symbolCut, string.Empty);
                    }

                    if (string.IsNullOrEmpty(urlCompanyName))
                    {
                        searchUrl = $"https://www.zonebourse.com/recherche/instruments?q={name}";
                        urlCompanyName = await GetCompanyName(searchUrl, symbolCut, string.Empty);
                    }

                    if (!string.IsNullOrEmpty(urlCompanyName))
                    {
                        Console.WriteLine($"Company Name: {urlCompanyName}");

                        string agendaUrl = $"https://www.zonebourse.com{urlCompanyName.TrimEnd('/')}/agenda/";
                        List<string> dates = await GetFinancialDates(agendaUrl);

                        string format = "dd/MM/yyyy"; // Format attendu
                        CultureInfo culture = CultureInfo.InvariantCulture; // Culture indépendante

                        indice.DatesExercicesFinancieres = dates
                            .Select(date =>
                            {
                                DateTime parsedDate;
                                return DateTime.TryParseExact(date, format, culture, DateTimeStyles.None, out parsedDate)
                                    ? parsedDate
                                    : (DateTime?)null;
                            })
                            .Where(d => d.HasValue) // Filtre les valeurs null (dates invalides)
                            .Select(d => d.Value)
                            .ToArray();
                    }
                    else
                    {
                        Console.WriteLine("Company not found.");
                    }

                    dbContext.Indices.Update(indice);
                }
            }
            dbContext.SaveChanges();
        }
    }

    static async Task<string> GetCompanyName(string url, string symbol, string bourse)
    {
        using HttpClient client = new HttpClient();
        var response = await client.GetStringAsync(url);

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(response);

        HtmlNode node;

        if (bourse != null)
        {
            node = doc.DocumentNode.SelectSingleNode($"//tr[td[normalize-space() = '{symbol}'] and td[contains(normalize-space(), '{bourse}')]]");
        }
        else
        {
            node = doc.DocumentNode.SelectSingleNode($"//tr[td[normalize-space() = '{symbol}']]");
        }

        if (node == null)
        {
            node = doc.DocumentNode.SelectSingleNode($"//tr[td[contains(normalize-space(), '{symbol}')] and td[contains(normalize-space(), '{bourse}')]]");
        }

        string href = string.Empty;

        if (node != null)
        {
            // Sélectionner le premier lien <a> dans ce <tr>
            var linkNode = node.SelectSingleNode(".//a[@href]");

            if (linkNode != null)
            {
                href = linkNode.GetAttributeValue("href", string.Empty);
                Console.WriteLine("Lien trouvé : " + href);
            }
            else
            {
                Console.WriteLine("Aucun lien <a> trouvé dans le <tr>.");
            }
        }
        else
        {
            Console.WriteLine("Aucun <tr> correspondant trouvé.");
        }
        return href;
    }

    static async Task<List<string>> GetFinancialDates(string url)
    {
        List<string> dates = new List<string>();
        using (HttpClient client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(30); // Définir un délai d'attente de 30 secondes

            try
            {
                // Vérifier le statut de la réponse avant de lire le contenu
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erreur HTTP: {response.StatusCode}");
                    return dates;
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                // Traiter la page HTML
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseContent);

                // Sélectionner les éléments contenant les dates financières
                var nodes = doc.DocumentNode.SelectNodes("//div[contains(@id, 'next-events-card')]//table[contains(@class, 'table--bordered')]//td[@class='table-child--w130']");

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        dates.Add(node.InnerText.Trim());
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Une erreur s'est produite lors de la demande HTTP: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Une erreur s'est produite: {e.Message}");
            }
        }
        return dates;
    }

    //static async Task<List<string>> GetFinancialDates(string url)
    //{
    //    List<string> dates = new List<string>();
    //    using HttpClient client = new HttpClient();
    //    var response = await client.GetStringAsync(url);

    //    HtmlDocument doc = new HtmlDocument();
    //    doc.LoadHtml(response);

    //    var nodes = doc.DocumentNode.SelectNodes("//div[contains(@id, 'next-events-card')]//table[contains(@class, 'table--bordered')]//td[@class='table-child--w130']");

    //    if (nodes != null)
    //    {
    //        foreach (var node in nodes)
    //        {
    //            dates.Add(node.InnerText.Trim());
    //        }
    //    }
    //    return dates;
    //}

    // Méthode pour déduire une recommandation basée sur RSI
    private async Task<string> GetRecommendationBasedOnRSI(decimal rsi)
    {
        //// Calcul inversé
        //return rsi switch
        //{
        //    < 30 => "Strong Sell",    // Survendu
        //    < 40 => "Sell",
        //    > 70 => "Strong Buy",   // Suracheté
        //    > 60 => "Buy",
        //    _ => "Hold"              // Zone neutre
        //};

        // Calcul regulier
        return rsi switch
        {
            < 30 => "Strong Buy",    // Survendu
            < 40 => "Buy",
            > 70 => "Strong Sell",   // Suracheté
            > 60 => "Sell",
            _ => "Hold"              // Zone neutre
        };
    }

    public static float[] CalculateSMA(float[] closePrices, int period)
    {
        if (closePrices == null || closePrices.Length == 0 || period <= 0)
            return Array.Empty<float>();

        int length = closePrices.Length;
        var sma = new float[length];
        float sum = 0;

        for (int i = 0; i < length; i++)
        {
            sum += closePrices[i];

            if (i >= period - 1)
            {
                sma[i] = sum / period;
                sum -= closePrices[i - period + 1];
            }
            else
            {
                sma[i] = 0; // Pas assez de données
            }
        }

        return sma;
    }

    public static float[] CalculateRSI(float[] closePrices, int period)
    {
        if (closePrices == null || closePrices.Length == 0 || period <= 0)
            return Array.Empty<float>();

        int length = closePrices.Length;
        var rsi = new float[length];
        float gain = 0, loss = 0;

        if (length < period)
            period = length;

        // Calcul des gains et pertes initiaux
        for (int i = 1; i < period; i++)
        {
            float change = closePrices[i] - closePrices[i - 1];
            if (change > 0)
                gain += change;
            else if (change < 0)
                loss -= change;
        }

        gain /= (period - 1);
        loss /= (period - 1);

        // Éviter la division par zéro
        rsi[period - 1] = loss == 0 ? 100 : 100 - (100 / (1 + gain / loss));

        // Calcul du RSI pour le reste des données
        for (int i = period; i < length; i++)
        {
            float change = closePrices[i] - closePrices[i - 1];
            gain = (gain * (period - 1) + Math.Max(change, 0)) / period;
            loss = (loss * (period - 1) + Math.Max(-change, 0)) / period;

            rsi[i] = loss == 0 ? 100 : 100 - (100 / (1 + gain / loss));
        }

        return rsi;
    }

    //private async Task<DateTime[]> GetFinancialDataAsync(Indice indice)
    //{
    //    // Charger les horaires de la bourse
    //    FuseHoraire fuseHoraire = GetHoraireOverture(indice.Bourse.ToLower());

    //    // Convertir la date donnée dans le fuseau horaire de la bourse
    //    DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

    //    var datesList = new List<DateTime>();

    //    var indiceRef = _dbContext.Indices.Where(i => i.Id == indice.Id).SingleOrDefault();

    //    if (indiceRef == null) 
    //    { 
    //        return datesList.ToArray();
    //    }

    //    if(indiceRef.DatesExercicesFinancieres.FirstOrDefault() != DateTime.Parse("0001-01-01 00:00:00"))
    //    {
    //        DateTime[] dates = indiceRef.DatesExercicesFinancieres;

    //        foreach (DateTime date in dates)
    //        {
    //            if (date < dateDansLeFuseau)
    //            {
    //                datesList.Add(date.AddYears(1));
    //            }

    //            datesList.Add(date);
    //        }
    //    }

    //    return datesList.ToArray();

    //// Yahoo Finance Api implementation
    //try
    //{
    //    // Récupérer les données historiques via Yahoo Finance API
    //    var history = await Yahoo.GetHistoricalAsync(symbol, DateTime.Now.AddYears(-1), DateTime.Now, Period.Daily);

    //    if (history != null && history.Count > 0)
    //    {
    //        // Filtrer les données pour obtenir des informations sur la période trimestrielle
    //        var quarterlyData = GetQuarterlyData(history);

    //        // Afficher ou traiter les données trimestrielles
    //        foreach (var quarter in quarterlyData)
    //        {
    //            Console.WriteLine($"Quarter Ending: {quarter.Key.ToShortDateString()}");
    //            foreach (var data in quarter.Value)
    //            {
    //                datesList.Add(data.DateTime.AddYears(1));
    //                Console.WriteLine($"Date: {data.DateTime.AddYears(1)}, Close: {data.Close}");
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Console.WriteLine($"No historical data found for {symbol}.");
    //    }
    //}
    //catch (Exception ex)
    //{
    //    Console.WriteLine($"Error while fetching quarterly data for {symbol}: {ex.Message}");
    //}

    //return datesList.ToArray(); // Retourne la liste des dates sous forme de tableau


    //// Alpha Vantage Api implementation
    //string baseUrl = "https://www.alphavantage.co/query";
    //string function = "BALANCE_SHEET"; // Utilisez une fonction adaptée, comme BALANCE_SHEET ou INCOME_STATEMENT

    //string url = $"{baseUrl}?function={function}&symbol={symbol}&entitlement=delayed&apikey={_apiKey}";

    //using (HttpClient client = new HttpClient())
    //{
    //    try
    //    {
    //        HttpResponseMessage response = await client.GetAsync(url);
    //        response.EnsureSuccessStatusCode();

    //        string responseBody = await response.Content.ReadAsStringAsync();
    //        JObject data = JObject.Parse(responseBody);

    //        // Exemple : Parcourir les dates des bilans
    //        var quarterlyReports = data["quarterlyReports"];
    //        if (quarterlyReports != null)
    //        {
    //            foreach (var report in quarterlyReports)
    //            {
    //                string fiscalDateEnding = report["fiscalDateEnding"]?.ToString();
    //                if (DateTime.TryParseExact(fiscalDateEnding, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fiscalDate))
    //                {
    //                    if (fiscalDate.AddYears(1) > DateTime.Now)
    //                        datesList.Add(fiscalDate.AddYears(1));
    //                }
    //            }
    //        }
    //        else
    //        {
    //            Console.WriteLine("Aucun rapport trimestriel trouvé.");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Erreur : {ex.Message}");
    //    }

    //    return datesList.ToArray(); // Convertit la liste en tableau
    //}
    //}

    //static async Task<List<EarningsInfo>> GetEarningsFromZoneBourse(int pageNumber)
    //{
    //    string url = "https://www.zonebourse.com/bourse/agenda/financier/amerique-du-nord/?p={pageNumber}";
    //    var earningsList = new List<EarningsInfo>();

    //    using (HttpClient client = new HttpClient())
    //    {
    //        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

    //        try
    //        {
    //            var response = await client.GetStringAsync(url);
    //            var htmlDoc = new HtmlDocument();
    //            htmlDoc.LoadHtml(response);

    //            // Sélection des lignes du tableau des résultats financiers
    //            var rows = htmlDoc.DocumentNode.SelectNodes("//table[contains(@class, 'table table--small table--bordered table--hover table--fixed table--stock')]//tr");

    //            if (rows != null)
    //            {
    //                foreach (var row in rows)
    //                {
    //                    var columns = row.SelectNodes("td");
    //                    if (columns != null && columns.Count >= 3)
    //                    {
    //                        string dateText = columns[2].InnerText.Trim();
    //                        string companyName = columns[1].InnerText.Trim();

    //                        // Récupération de l'URL vers la fiche entreprise
    //                        var linkNode = columns[1].SelectSingleNode(".//a");
    //                        string companyUrl = linkNode?.GetAttributeValue("href", null);

    //                        if (DateTime.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime earningsDate) && companyUrl != null)
    //                        {
    //                            string fullCompanyUrl = "https://www.zonebourse.com" + companyUrl;
    //                            string symbol = await GetSymbolFromCompanyPage(fullCompanyUrl);

    //                            earningsList.Add(new EarningsInfo
    //                            {
    //                                Date = earningsDate,
    //                                Company = companyName,
    //                                Symbol = symbol,
    //                                Url = fullCompanyUrl
    //                            });
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        catch (HttpRequestException ex)
    //        {
    //            Console.WriteLine($"Erreur HTTP: {ex.Message}");
    //        }
    //    }

    //    return earningsList;
    //}

    //static async Task<string> GetSymbolFromCompanyPage(string companyUrl)
    //{
    //    using (HttpClient client = new HttpClient())
    //    {
    //        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

    //        try
    //        {
    //            var response = await client.GetStringAsync(companyUrl);
    //            var htmlDoc = new HtmlDocument();
    //            htmlDoc.LoadHtml(response);

    //            // Extraction du symbole (souvent dans un méta tag ou dans un div)
    //            var symbolNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='keywords']");

    //            if (symbolNode != null)
    //            {
    //                string title = symbolNode.GetAttributeValue("content", "").Trim();
    //                string[] parts = title.Split(',');

    //                for (int i = 0; i < parts.Length - 1; i++)  // Évite l'erreur d'index hors limites
    //                {
    //                    string text = parts[i].Trim();
    //                    if (parts[i].Trim().Contains("cours") && parts[i].Trim().Contains("Action"))
    //                    {
    //                        return parts[i + 1].Trim();  // Retourne l'élément suivant
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Erreur lors de la récupération du symbole : {ex.Message}");
    //        }
    //    }

    //    return "N/A";
    //}

    // Modèle pour stocker les informations des résultats financiers

    //private async Task<DateTime[]> GetFinancialDataYahooAsync(Indice indice, BourseContext dbContext)
    //{
    //    // Yahoo Finance Api implementation
    //    //var datesList = new List<DateTime>();

    //    string symbol = indice.Symbol;

    //    try
    //    {
    //        if (string.IsNullOrWhiteSpace(symbol))
    //            throw new ArgumentException("Le symbole ne peut pas être vide ou null.");

    //        FlurlHttp.Configure(settings =>
    //        {
    //        settings.Timeout = TimeSpan.FromSeconds(60); // Increase timeout to 60 seconds
    //        });

    //        var securitiesQuery = Yahoo.Symbols(symbol);

    //        if (securitiesQuery == null)
    //        {
    //            throw new NullReferenceException("L'objet Yahoo.Symbols(symbol) est null.");
    //        }

    //        var securitiesFields = securitiesQuery.Fields(Field.EarningsTimestamp, Field.EarningsTimestampStart, Field.EarningsTimestampEnd);

    //        if (securitiesFields == null)
    //        {
    //            throw new NullReferenceException("L'appel à .Fields() a retourné null.");
    //        }

    //        var securities = await securitiesFields.QueryAsync();
    //        if (securities == null)
    //        {
    //            throw new NullReferenceException("L'appel à QueryAsync() a retourné null.");
    //        }

    //        //// Appel à l'API Yahoo pour obtenir les données fondamentales
    //        //var securities = await Yahoo.Symbols(symbol)
    //        //    .Fields(Field.EarningsTimestamp, Field.EarningsTimestampStart, Field.EarningsTimestampEnd)
    //        //    .QueryAsync();

    //        // Vérifie si le symbole existe dans la réponse
    //        if (securities.ContainsKey(symbol))
    //        {
    //            var security = securities[symbol];
    //            List<DateTime> earningsDates = new();

    //            // Extraction des dates des résultats financiers
    //            if (security.Fields.TryGetValue("EarningsTimestamp", out object valueEarningsTimestamp))
    //            {
    //                earningsDates.Add(DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(valueEarningsTimestamp)).LocalDateTime);
    //            }
    //            if (security.Fields.TryGetValue("EarningsTimestampStart", out object valueEarningsTimestampStart))
    //            {
    //                earningsDates.Add(DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(valueEarningsTimestampStart)).LocalDateTime);
    //            }
    //            if (security.Fields.TryGetValue("EarningsTimestampEnd", out object valueEarningsTimestampEnd))
    //            {
    //                earningsDates.Add(DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(valueEarningsTimestampEnd)).LocalDateTime);
    //            }

    //            return earningsDates.ToArray();
    //        }

    //var securitiesQuery = Yahoo.Symbols(symbol);

    //if (securitiesQuery == null)
    //{
    //    throw new NullReferenceException("L'objet Yahoo.Symbols(symbol) est null.");
    //}

    //var securitiesFields = securitiesQuery.Fields(Field.EarningsTimestamp);

    //if (securitiesFields == null)
    //{
    //    throw new NullReferenceException("L'appel à .Fields() a retourné null.");
    //}

    //try
    //{
    //    var securities = await securitiesFields.QueryAsync();
    //    if (securities == null)
    //    {
    //        throw new NullReferenceException("L'appel à QueryAsync() a retourné null.");
    //    }

    //    if (securities.ContainsKey(symbol))
    //    {
    //        var data = securities[symbol];

    //        DateTime earningDate;

    //        if (data.Fields.TryGetValue("EarningsTimestamp", out object value))
    //        {
    //            earningDate = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value)).DateTime;

    //        }
    //        else
    //        {
    //            earningDate = new DateTime();
    //        }
    //        datesList.Add(earningDate);
    //    }
    //    else
    //    {
    //        Console.WriteLine($"Symbol '{symbol}' not found in the response.");
    //    }

    //    await Task.Delay(1000);
    //}
    //catch (Exception ex)
    //{
    //    Console.WriteLine("Erreur lors de QueryAsync : " + ex.Message);
    //}

    //    }
    //    catch (Flurl.Http.FlurlHttpException ex)
    //    {
    //        Console.WriteLine($"Erreur HTTP pour {symbol} : {ex.Message}");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Erreur pour {symbol} : {ex.Message}");
    //    }

    //    return Array.Empty<DateTime>();

    //    //return datesList.ToArray(); // Retourne la liste des dates sous forme de tableau
    //}

    // Fonction pour extraire les données trimestrielles à partir des données quotidiennes
    //private Dictionary<DateTime, List<Candle>> GetQuarterlyData(IReadOnlyList<Candle> history)
    //{
    //    // Grouper les données par trimestre (par exemple, chaque période de trois mois)
    //    var quarterlyData = new Dictionary<DateTime, List<Candle>>();

    //    foreach (var data in history)
    //    {
    //        // Obtenez le trimestre correspondant à la date
    //        var quarter = new DateTime(data.DateTime.Year, (data.DateTime.Month - 1) / 3 * 3 + 1, 1); // Premier jour du trimestre

    //        if (!quarterlyData.ContainsKey(quarter))
    //        {
    //            quarterlyData[quarter] = new List<Candle>();
    //        }

    //        quarterlyData[quarter].Add(data);
    //    }

    //    return quarterlyData;
    //}

    //private async Task<DateTime[]> GetEarningsDates(string symbol)
    //{
    //    var financialDates = new List<DateTime>();

    //    // Alpha Vantage Api implementation
    //    try
    //    {
    //        // Construire l'URL pour obtenir les états financiers
    //        string url = $"https://www.alphavantage.co/query?function=INCOME_STATEMENT&symbol={symbol}&entitlement=delayed&apikey={_apiKey}";

    //        using (HttpClient client = new HttpClient())
    //        {
    //            // Envoyer une requête GET
    //            HttpResponseMessage response = await client.GetAsync(url);

    //            if (response.IsSuccessStatusCode)
    //            {
    //                string jsonResponse = await response.Content.ReadAsStringAsync();
    //                var jsonData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

    //                // Vérifier si les rapports trimestriels ou annuels existent
    //                if (jsonData.TryGetProperty("quarterlyReports", out JsonElement quarterlyReports))
    //                {
    //                    foreach (var report in quarterlyReports.EnumerateArray())
    //                    {
    //                        string fiscalDateEndingString = report.GetProperty("fiscalDateEnding").GetString();

    //                        if (DateTime.TryParseExact(fiscalDateEndingString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fiscalDateEnding))
    //                        {
    //                            if (fiscalDateEnding.AddYears(1) > DateTime.Now)
    //                                financialDates.Add(fiscalDateEnding.AddYears(1));
    //                        }
    //                    }
    //                }
    //                else if (jsonData.TryGetProperty("annualReports", out JsonElement annualReports))
    //                {
    //                    foreach (var report in annualReports.EnumerateArray())
    //                    {
    //                        string fiscalDateEndingString = report.GetProperty("fiscalDateEnding").GetString();

    //                        if (DateTime.TryParseExact(fiscalDateEndingString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fiscalDateEnding))
    //                        {
    //                            if (fiscalDateEnding.AddYears(1) > DateTime.Now)
    //                                financialDates.Add(fiscalDateEnding.AddYears(1));
    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    Console.WriteLine($"No financial statement data found for {symbol}.");
    //                }
    //            }
    //            else
    //            {
    //                Console.WriteLine($"API call failed for {symbol}: {response.StatusCode}");
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error fetching financial statement data for {symbol}: {ex.Message}");
    //    }

    //    // Retourner les dates au format tableau
    //    return financialDates.ToArray();
    //}

    //private async Task GetAnalysisIndiceYhaooFromRSI(Indice indice)
    //{
    //    // Charger les horaires de la bourse
    //    FuseHoraire fuseHoraire = GetHoraireOverture(indice.Bourse.ToLower());
    //    // Convertir la date donnée dans le fuseau horaire de la bourse
    //    DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuseHoraire.TimeZoneInfo);

    //    try
    //    {
    //        decimal rsi = 50;
    //        string date = DateOnly.FromDateTime(dateDansLeFuseau).ToString();

    //        if(indice.TrainingData.Count > 0)
    //        {
    //            // Convertir RSI en decimal
    //            rsi = (decimal)indice.TrainingData.OrderByDescending(t => t.Date).SingleOrDefault().RSI_14;
    //            date = indice.TrainingData.OrderByDescending(t => t.Date).SingleOrDefault().Date.ToString();
    //        }

    //        // Générer une recommandation basée sur RSI
    //        string recommendation = await GetRecommendationBasedOnRSI(rsi);

    //        // Mise à jour de l'indice dans la base de données
    //        indice.DateUpdated = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    //        indice.Raccomandation = recommendation;

    //        _dbContext.Indices.Update(indice);
    //        await _dbContext.SaveChangesAsync();

    //        Console.WriteLine($"RSI analysis updated for {indice.Symbol}: {recommendation} (RSI: {rsi})");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error while fetching RSI analysis for {indice.Symbol}: {ex.Message}");
    //    }
    //}

    //private async Task GetAnalysisIndiceFromRSI(Indice indice)
    //{
    //    try
    //    {
    //        // Construire l'URL pour récupérer les données RSI
    //        string symbol = indice.Symbol;
    //        string url = $"https://www.alphavantage.co/query?function=RSI&symbol={symbol}&entitlement=delayed&interval=daily&time_period=14&series_type=close&apikey={_apiKey}";

    //        Console.WriteLine($"Fetching RSI analysis for: {symbol}");

    //        using (HttpClient client = new HttpClient())
    //        {
    //            // Envoyer une requête GET
    //            HttpResponseMessage response = await client.GetAsync(url);

    //            if (response.IsSuccessStatusCode)
    //            {
    //                string jsonResponse = await response.Content.ReadAsStringAsync();
    //                var jsonData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

    //                // Vérifier si une erreur est retournée
    //                if (jsonData.TryGetProperty("Note", out JsonElement note))
    //                {
    //                    Console.WriteLine($"API Note: {note.GetString()}");
    //                    return;
    //                }

    //                // Vérifier si les données RSI sont présentes
    //                if (jsonData.TryGetProperty("Technical Analysis: RSI", out JsonElement rsiData))
    //                {
    //                    // Obtenir la valeur RSI la plus récente
    //                    var latestEntry = rsiData.EnumerateObject().FirstOrDefault();
    //                    if (!string.IsNullOrEmpty(latestEntry.Name))
    //                    {
    //                        string date = latestEntry.Name;
    //                        string rsiValue = latestEntry.Value.GetProperty("RSI").GetString();

    //                        // Convertir RSI en nombre
    //                        decimal rsi = decimal.Parse(rsiValue, CultureInfo.InvariantCulture);

    //                        // Générer une recommandation basée sur RSI
    //                        string recommendation = await GetRecommendationBasedOnRSI(rsi);

    //                        // Mise à jour de l'indice dans la base de données
    //                        indice.DateUpdated = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    //                        indice.Raccomandation = recommendation;

    //                        _dbContext.Indices.Update(indice);
    //                        await _dbContext.SaveChangesAsync();

    //                        Console.WriteLine($"RSI analysis updated for {symbol}: {recommendation} (RSI: {rsi})");
    //                    }
    //                    else
    //                    {
    //                        Console.WriteLine("No valid RSI data found.");
    //                    }
    //                }
    //                else
    //                {
    //                    Console.WriteLine("RSI data not found in the API response.");
    //                }
    //            }
    //            else
    //            {
    //                Console.WriteLine($"API call failed for {symbol}: {response.StatusCode}");
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error while fetching RSI analysis for {indice.Symbol}: {ex.Message}");
    //    }
    //}

    //// Méthode pour convertir RGB en HEX
    //static string RgbToHex(string rgb)
    //{
    //    var match = System.Text.RegularExpressions.Regex.Match(rgb, @"rgb\((\d+),\s*(\d+),\s*(\d+)\)");
    //    if (match.Success)
    //    {
    //        int r = int.Parse(match.Groups[1].Value);
    //        int g = int.Parse(match.Groups[2].Value);
    //        int b = int.Parse(match.Groups[3].Value);
    //        return $"#{r:X2}{g:X2}{b:X2}";
    //    }
    //    return "#000000"; // Default noir si non valide
    //}

    //static string ExtractDateFromFilePath(string filePath)
    //{
    //    // Supposons que le format du fichier est toujours TSX_YYYYMMDD.txt
    //    // On isole le segment "YYYYMMDD" entre "_" et "."
    //    int underscoreIndex = filePath.IndexOf('_') + 1;
    //    int dotIndex = filePath.LastIndexOf('.');
    //    return filePath.Substring(underscoreIndex, dotIndex - underscoreIndex);
    //}

    #endregion#

    #region horaireBourses
    // Obtenir horaire ouverture bourses
    public static FuseHoraire GetHoraireOverture(string bourse)
    {
        // Ajustez ces horaires selon la bourse choisie
        FuseHoraire fuseHoraire;

        switch (bourse.ToLower())
        {
            case "tsx":
                fuseHoraire = new FuseHoraire
                {
                    TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
                    Ouverture = new TimeSpan(9, 30, 0),  // 9h30
                    Fermeture = new TimeSpan(16, 0, 0),  // 16h00
                    JoursFeries = GetJoursFeriesToronto()
                };
                break;

            case "nyse":
            case "nasdaq":
            case "amex":
                fuseHoraire = new FuseHoraire
                {
                    TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
                    Ouverture = new TimeSpan(9, 30, 0),  // 9h30
                    Fermeture = new TimeSpan(16, 0, 0),  // 16h00
                    JoursFeries = GetJoursFeriesUSA()  // Jours fériés spécifiques à NYSE
                };
                break;

            default:
                throw new ArgumentException("Bourse non reconnue.");
        }

        // Retourne la date donnée dans le fuseau horaire de la bourse
        return fuseHoraire;
    }

    public static bool EstDansLesHorairesBourse(string bourse)
    {
        DateTime date = DateTime.UtcNow;

        // Ajustez ces horaires selon la bourse choisie
        TimeZoneInfo fuseauHoraireBourse;
        TimeSpan ouverture, fermeture;
        HashSet<DateTime> joursFeries = new HashSet<DateTime>();

        switch (bourse.ToLower())
        {
            case "tsx":
                fuseauHoraireBourse = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                ouverture = new TimeSpan(9, 30, 0);  // 9h30
                fermeture = new TimeSpan(16, 0, 0);  // 16h00
                joursFeries = GetJoursFeriesToronto();  // Jours fériés spécifiques à NYSE
                break;
            case "nyse":
                fuseauHoraireBourse = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                ouverture = new TimeSpan(9, 30, 0);  // 9h30
                fermeture = new TimeSpan(16, 0, 0);  // 16h00
                joursFeries = GetJoursFeriesUSA();  // Jours fériés spécifiques à NYSE
                break;
            case "nasdaq":
                fuseauHoraireBourse = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                ouverture = new TimeSpan(9, 30, 0);  // 9h30
                fermeture = new TimeSpan(16, 0, 0);  // 16h00
                joursFeries = GetJoursFeriesUSA();   // Jours fériés spécifiques à LSE
                break;
            case "amex":
                fuseauHoraireBourse = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                ouverture = new TimeSpan(9, 30, 0);  // 9h30
                fermeture = new TimeSpan(16, 0, 0);  // 16h00
                joursFeries = GetJoursFeriesUSA(); // Jours fériés spécifiques à Euronext
                break;
            default:
                throw new ArgumentException("Bourse non reconnue.");
        }

        // Convertir la date donnée dans le fuseau horaire de la bourse
        DateTime dateDansLeFuseau = TimeZoneInfo.ConvertTime(date, fuseauHoraireBourse);


        // Vérifier si c'est un jour ouvré (lundi à vendredi) et non un jour férié
        if (dateDansLeFuseau.DayOfWeek == DayOfWeek.Saturday || dateDansLeFuseau.DayOfWeek == DayOfWeek.Sunday)
        {
            return false; // Fermé le week-end
        }
        if (joursFeries.Contains(dateDansLeFuseau.Date))
        {
            return false; // Fermé les jours fériés
        }

        // Vérifier si l'heure est dans la plage d'ouverture
        TimeSpan heureActuelle = dateDansLeFuseau.TimeOfDay;
        return heureActuelle >= ouverture && heureActuelle <= fermeture;
    }

    // Liste des jours fériés pour TORONTO (à adapter selon l'année)
    private static HashSet<DateTime> GetJoursFeriesToronto()
    {
        return new HashSet<DateTime>
        {
            new DateTime(DateTime.Now.Year, 1, 1),    // Nouvel An
            GetThirdMondayOfFebruary(DateTime.Now.Year),  // Jour de la Famille
            GetGoodFriday(DateTime.Now.Year),  // Vendredi saint
            GetThirdMondayOfMay(DateTime.Now.Year),  // Fête de la Reine
            new DateTime(DateTime.Now.Year, 7, 1),  // Fête du Canada
            GetFirstMondayOfAugust(DateTime.Now.Year),  // Congé civique
            GetFirstMondayOfSeptember(DateTime.Now.Year),  // Fête du Travail
            GetSecondMondayOfOctober(DateTime.Now.Year),  // Action de grâce
            new DateTime(DateTime.Now.Year, 12, 25),  // Noël
            new DateTime(DateTime.Now.Year, 12, 26),  // Lendemain de Noël
            // Ajouter d'autres jours fériés ici...
        };
    }

    // Liste des jours fériés pour NYSE-NASDAQ-AMEX (à adapter selon l'année)
    private static HashSet<DateTime> GetJoursFeriesUSA()
    {
        return new HashSet<DateTime>
        {
            new DateTime(DateTime.Now.Year, 1, 1),    // Nouvel An
            GetThirdMondayOfJanuary(DateTime.Now.Year),  // Martin Luther King Jr. Day
            GetThirdMondayOfFebruary(DateTime.Now.Year),  // Washington's Birthday
            GetGoodFriday(DateTime.Now.Year),  // Good Friday
            GetLastMondayOfMay(DateTime.Now.Year),  // Memorial Day
            new DateTime(DateTime.Now.Year, 6, 19),  // Juneteenth
            new DateTime(DateTime.Now.Year, 7, 4).AddHours(13),  // Independence Day
            GetFirstMondayOfSeptember(DateTime.Now.Year),  // Labor Day
            GetFourthThursdayOfNovember(DateTime.Now.Year),  // Thanksgiving
            GetDayAfterFourthThursdayOfNovember(DateTime.Now.Year).AddHours(13),  // Day after Thanksgiving
            new DateTime(DateTime.Now.Year, 12, 25),  // Christmas Day
            // Ajouter d'autres jours fériés ici...
        };
    }
    #endregion

    #region jourFerieFixes
    // Calcule des jours fériés pas fixes
    static DateTime GetThirdMondayOfJanuary(int year)
    {
        // Début du mois de février
        DateTime firstDayOfJanuary = new DateTime(year, 1, 1);

        // Trouver le premier lundi
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)firstDayOfJanuary.DayOfWeek + 7) % 7;
        DateTime firstMonday = firstDayOfJanuary.AddDays(daysUntilMonday);

        // Ajouter 14 jours pour arriver au troisième lundi
        DateTime thirdMonday = firstMonday.AddDays(14);

        return thirdMonday;
    }

    static DateTime GetThirdMondayOfFebruary(int year)
    {
        // Début du mois de février
        DateTime firstDayOfFebruary = new DateTime(year, 2, 1);

        // Trouver le premier lundi
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)firstDayOfFebruary.DayOfWeek + 7) % 7;
        DateTime firstMonday = firstDayOfFebruary.AddDays(daysUntilMonday);

        // Ajouter 14 jours pour arriver au troisième lundi
        DateTime thirdMonday = firstMonday.AddDays(14);

        return thirdMonday;
    }

    static DateTime GetGoodFriday(int year)
    {
        DateTime easterSunday = GetEasterSunday(year);
        DateTime goodFriday = easterSunday.AddDays(-2); // Vendredi Saint est 2 jours avant Pâques
        return goodFriday;
    }

    static DateTime GetEasterSunday(int year)
    {
        // Algorithme de Meeus/Jones/Butcher
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;

        return new DateTime(year, month, day);
    }

    static DateTime GetThirdMondayOfMay(int year)
    {
        // Début du mois de février
        DateTime firstDayOfMay = new DateTime(year, 5, 1);

        // Trouver le premier lundi
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)firstDayOfMay.DayOfWeek + 7) % 7;
        DateTime firstMonday = firstDayOfMay.AddDays(daysUntilMonday);

        // Ajouter 14 jours pour arriver au troisième lundi
        DateTime thirdMonday = firstMonday.AddDays(14);

        return thirdMonday;
    }

    static DateTime GetLastMondayOfMay(int year)
    {
        // Dernier jour de mai
        DateTime lastDayOfMay = new DateTime(year, 5, 31);

        // Revenir en arrière jusqu'au lundi
        int daysToMonday = (int)lastDayOfMay.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToMonday < 0) daysToMonday += 7; // Ajuster si négatif
        DateTime lastMonday = lastDayOfMay.AddDays(-daysToMonday);

        return lastMonday;
    }

    static DateTime GetFirstMondayOfAugust(int year)
    {
        // Début du mois d'août
        DateTime firstDayOfAugust = new DateTime(year, 8, 1);

        // Calculer le premier lundi
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)firstDayOfAugust.DayOfWeek + 7) % 7;
        DateTime firstMonday = firstDayOfAugust.AddDays(daysUntilMonday);

        return firstMonday;
    }

    static DateTime GetFirstMondayOfSeptember(int year)
    {
        // Début du mois d'août
        DateTime firstDayOfSeptember = new DateTime(year, 9, 1);

        // Calculer le premier lundi
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)firstDayOfSeptember.DayOfWeek + 7) % 7;
        DateTime firstMonday = firstDayOfSeptember.AddDays(daysUntilMonday);

        return firstMonday;
    }

    static DateTime GetSecondMondayOfOctober(int year)
    {
        // Début du mois d'octobre
        DateTime firstDayOfOctober = new DateTime(year, 10, 1);

        // Calculer le premier lundi
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)firstDayOfOctober.DayOfWeek + 7) % 7;
        DateTime firstMonday = firstDayOfOctober.AddDays(daysUntilMonday);

        // Ajouter 7 jours pour obtenir le deuxième lundi
        DateTime secondMonday = firstMonday.AddDays(7);

        return secondMonday;
    }

    static DateTime GetFourthThursdayOfNovember(int year)
    {
        // Début du mois de novembre
        DateTime firstDayOfNovember = new DateTime(year, 11, 1);

        // Trouver le premier jeudi
        int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)firstDayOfNovember.DayOfWeek + 7) % 7;
        DateTime firstThursday = firstDayOfNovember.AddDays(daysUntilThursday);

        // Ajouter 21 jours pour atteindre le quatrième jeudi
        DateTime fourthThursday = firstThursday.AddDays(21);

        return fourthThursday;
    }

    static DateTime GetDayAfterFourthThursdayOfNovember(int year)
    {
        // Calculer le quatrième jeudi de novembre
        DateTime fourthThursday = GetFourthThursdayOfNovember(year);

        // Ajouter un jour
        DateTime dayAfter = fourthThursday.AddDays(1);

        return dayAfter;
    }
    #endregion#

}
