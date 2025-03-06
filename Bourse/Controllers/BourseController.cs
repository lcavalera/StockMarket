using Bourse.Interfaces;
using Bourse.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Bourse.Controllers
{
    public class BourseController : Controller
    {
        private readonly ILogger<BourseController> _logger;
        private readonly IIndiceService _service;
        private List<Indice> _indices;
        private DateTime _dateHistorique;
        private string _cheminFichier = "date.csv";
        private readonly IScheduledTaskService _scheduledTaskService;
        private readonly HttpClient client = new HttpClient();
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BourseController(ILogger<BourseController> logger, IIndiceService service, IScheduledTaskService scheduledTaskService, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _service = service;
            _scheduledTaskService = scheduledTaskService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: TSXController
        public async Task<IActionResult> Index(string filtre, string exchangeFiltre, string sortOrder, bool resethistorique = false)
        {
            if (resethistorique)
            {
                using (StreamWriter sw = new StreamWriter(_cheminFichier))
                {
                    sw.WriteLineAsync(DateTime.Now.ToString());
                }

                //await _service.DeleteHistoriqueRealPrices();
            }
            ViewBag.Exchanges = new List<dynamic> { new { Key = "NYC", Value = "New York" }, new { Key = "TOR", Value = "Toronto" } };

            ViewBag.CurrentSort = sortOrder;

            ViewBag.SymbolSortParm = String.IsNullOrEmpty(sortOrder) ? "symbol_desc" : "";
            ViewBag.PriceSortParm = sortOrder == "price_desc" ? "price_asc" : "price_desc";
            ViewBag.ChangeSortParm = sortOrder == "change_asc" ? "change_desc" : "change_asc";
            ViewBag.DatesExercFinancParm = sortOrder == "exercfinanc_asc" ? "exercfinanc_desc" : "exercfinanc_asc";
            ViewBag.BourseParm = sortOrder == "bourse_asc" ? "bourse_desc" : "bourse_asc";
            //ViewBag.ExchangeSortParm = sortOrder == "exchange_asc" ? "exchange_desc" : "exchange_asc";
            ViewBag.LabelSortParm = sortOrder == "label_asc" ? "label_desc" : "label_asc";
            ViewBag.ActionSortParm = sortOrder == "action_asc" ? "action_asc" : "action_desc";
            ViewBag.ProbSortParm = sortOrder == "prob_desc" ? "prob_asc" : "prob_desc";

            if (filtre is null)
            {
                using (StreamReader sr = new StreamReader(_cheminFichier))
                {
                    while (!sr.EndOfStream)
                    {
                        string ligne = await sr.ReadLineAsync();
                        if (!string.IsNullOrEmpty(ligne))
                        {
                            //while (ligne != null)
                            //{
                            _dateHistorique = DateTime.Parse(ligne);
                            //}
                        }
                    }
                }

                _indices = await _service.ObtenirTout();

            }
            else
            {
                ViewData["actifFiltre"] = filtre;
                _indices = await _service.ObtenirSelonName(filtre);
            }

            // Stocker le filtre actif
            ViewData["ExchangeFiltre"] = exchangeFiltre;

            if (!string.IsNullOrEmpty(exchangeFiltre))
            {
                if (exchangeFiltre == "TOR")
                {
                    _indices = _indices.Where(i => i.Exchange == exchangeFiltre).ToList();
                }
                else
                {
                    _indices = _indices.Where(i => i.Exchange != "TOR").ToList();
                }

            }

            ViewData["DateReset"] = _dateHistorique.ToString("yyyy-MM-dd");

            switch (sortOrder)
            {
                case "symbol_desc":
                    _indices = _indices.OrderByDescending(i => i.Symbol).ToList();
                    break;
                case "price_asc":
                    _indices = _indices.OrderBy(i => i.RegularMarketPrice).ToList();
                    break;
                case "price_desc":
                    _indices = _indices.OrderByDescending(i => i.RegularMarketPrice).ToList();
                    break;
                case "change_asc":
                    _indices = _indices.OrderBy(i => i.RegularMarketChange).ToList();
                    break;
                case "change_desc":
                    _indices = _indices.OrderByDescending(i => i.RegularMarketChange).ToList();
                    break;
                case "exercfinanc_asc":
                    _indices = _indices.OrderBy(i => i.DatesExercicesFinancieres.OrderBy(d=> d.Date).FirstOrDefault()).ToList();
                    break;
                case "exercfinanc_desc":
                    _indices = _indices.OrderByDescending(i => i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault()).ToList();
                    break;
                case "exchange_asc":
                    _indices = _indices.OrderBy(i => i.Exchange).ToList();
                    break;
                case "exchange_desc":
                    _indices = _indices.OrderByDescending(i => i.Exchange).ToList();
                    break;
                case "label_asc":
                    _indices = _indices.OrderBy(i => i.Label).ToList();
                    break;
                case "label_desc":
                    _indices = _indices.OrderByDescending(i => i.Label).ToList();
                    break;
                case "action_asc":
                    _indices = _indices.OrderBy(i => i.IsIncreasing).ToList();
                    break;
                case "action_desc":
                    _indices = _indices.OrderByDescending(i => i.IsIncreasing).ToList();
                    break;
                case "prob_asc":
                    _indices = _indices.OrderBy(i => i.Probability).ToList();
                    break;
                case "prob_desc":
                    _indices = _indices.OrderByDescending(i => i.Probability).ToList();
                    break;
                default:
                    _indices = _indices.OrderBy(i => i.Symbol).ToList();  // Par défaut, tri croissant sur le prix
                    break;
            }

            return _indices != null ?
                        View(_indices) :
                        Problem("Entité set 'symbols' est null.");
        }

        // GET: TSXController/Details/5
        [HttpGet("item")]
        public async Task<IActionResult> Details(string item, bool resethistorique = false)
        {

            Indice indice = await _service.ObtenirSelonSymbol(item);
            if (indice == null)
            {
                _logger.LogError($"Une erreur c'est produite lors de la récupération d'une indice. Symbol = {item}");
                return NotFound();

            }
            return View(indice);
        }

        // GET: TSXController/Forecasts
        public async Task<IActionResult> Forecasts(string filtre, string exchangeFiltre, string sortOrder, bool resethistorique = false)
        {
            if (resethistorique)
            {
                using (StreamWriter sw = new StreamWriter(_cheminFichier))
                {
                    sw.WriteLineAsync(DateTime.Now.ToString());
                }

                //await _service.DeleteHistoriqueRealPrices();
            }
            ViewBag.Exchanges = new List<dynamic> { new { Key = "NYC", Value = "New York" }, new { Key = "TOR", Value = "Toronto" } };

            ViewBag.CurrentSort = sortOrder;

            ViewBag.SymbolSortParm = String.IsNullOrEmpty(sortOrder) ? "symbol_desc" : "";
            ViewBag.PriceSortParm = sortOrder == "price_desc" ? "price_asc" : "price_desc";
            ViewBag.ChangeSortParm = sortOrder == "change_asc" ? "change_desc" : "change_asc";
            ViewBag.DatesExercFinancParm = sortOrder == "exercfinanc_asc" ? "exercfinanc_desc" : "exercfinanc_asc";
            ViewBag.BourseParm = sortOrder == "bourse_asc" ? "bourse_desc" : "bourse_asc";
            //ViewBag.ExchangeSortParm = sortOrder == "exchange_asc" ? "exchange_desc" : "exchange_asc";
            ViewBag.LabelSortParm = sortOrder == "label_asc" ? "label_desc" : "label_asc";
            ViewBag.ActionSortParm = sortOrder == "action_asc" ? "action_asc" : "action_desc";
            ViewBag.ProbSortParm = sortOrder == "prob_desc" ? "prob_asc" : "prob_desc";

            if (filtre is null)
            {
                using (StreamReader sr = new StreamReader(_cheminFichier))
                {
                    while (!sr.EndOfStream)
                    {
                        string ligne = await sr.ReadLineAsync();
                        if (!string.IsNullOrEmpty(ligne))
                        {
                            //while (ligne != null)
                            //{
                            _dateHistorique = DateTime.Parse(ligne);
                            //}
                        }
                    }
                }

                _indices = await _service.ObtenirTout();

            }
            else
            {
                ViewData["actifFiltre"] = filtre;
                _indices = await _service.ObtenirSelonName(filtre);
            }

            // Stocker le filtre actif
            ViewData["ExchangeFiltre"] = exchangeFiltre;

            if (!string.IsNullOrEmpty(exchangeFiltre))
            {
                if (exchangeFiltre == "TOR")
                {
                    _indices = _indices.Where(i => i.Exchange == exchangeFiltre).ToList();
                }
                else
                {
                    _indices = _indices.Where(i => i.Exchange != "TOR").ToList();
                }

            }

            _indices = _indices
                .Where(i =>
                    //(i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() > DateTime.Now &&
                    //i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() < DateTime.Now.AddDays(90) &&
                    //i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() != DateTime.MinValue) &&
                    //i.QuoteType != "ETF" &&
                    i.DatesExercicesFinancieres.Any(d => d.Date != DateTime.MinValue) &&
                    (i.DatesExercicesFinancieres != null || i.DatesExercicesFinancieres.Length > 0) &&
                    ((i.Raccomandation == "Strong Buy" || i.Raccomandation == "Buy") && (i.Probability > 0.65 && i.Probability != 0) ||
                    (i.Raccomandation == "Sell" || i.Raccomandation == "Strong Sell") && (i.Probability < 0.35 && i.Probability != 0) ||
                    (i.Raccomandation == "Hold" && (i.Probability > 0.75 || i.Probability < 0.20) && i.Probability != 0))
                )
                .OrderByDescending(i => i.Probability)  // Tri par Probability décroissant
                .ThenBy(i => i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault()) // Tri par la première date croissante
                .ToList();

            ViewData["DateReset"] = _dateHistorique.ToString("yyyy-MM-dd");

            switch (sortOrder)
            {
                case "symbol_desc":
                    _indices = _indices.OrderByDescending(i => i.Symbol).ToList();
                    break;
                case "price_asc":
                    _indices = _indices.OrderBy(i => i.RegularMarketPrice).ToList();
                    break;
                case "price_desc":
                    _indices = _indices.OrderByDescending(i => i.RegularMarketPrice).ToList();
                    break;
                case "change_asc":
                    _indices = _indices.OrderBy(i => i.RegularMarketChange).ToList();
                    break;
                case "change_desc":
                    _indices = _indices.OrderByDescending(i => i.RegularMarketChange).ToList();
                    break;
                case "exercfinanc_asc":
                    _indices = _indices.OrderBy(i => i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault()).ToList();
                    break;
                case "exercfinanc_desc":
                    _indices = _indices.OrderByDescending(i => i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault()).ToList();
                    break;
                case "exchange_asc":
                    _indices = _indices.OrderBy(i => i.Exchange).ToList();
                    break;
                case "exchange_desc":
                    _indices = _indices.OrderByDescending(i => i.Exchange).ToList();
                    break;
                case "label_asc":
                    _indices = _indices.OrderBy(i => i.Label).ToList();
                    break;
                case "label_desc":
                    _indices = _indices.OrderByDescending(i => i.Label).ToList();
                    break;
                case "action_asc":
                    _indices = _indices.OrderBy(i => i.IsIncreasing).ToList();
                    break;
                case "action_desc":
                    _indices = _indices.OrderByDescending(i => i.IsIncreasing).ToList();
                    break;
                case "prob_asc":
                    _indices = _indices.OrderBy(i => i.Probability).ToList();
                    break;
                case "prob_desc":
                    _indices = _indices.OrderByDescending(i => i.Probability).ToList();
                    break;
                //default:
                //    _indices = _indices.OrderBy(i => i.Symbol).ToList();  // Par défaut, tri croissant sur le prix
                //    break;
            }



            // À reactiver

            //if( _indices != null)
            //{
            //    foreach (var indice in _indices) 
            //    {
            //        await _service.GetImageAnalysisIndice(indice.Id);
            //    }
            //}

            return _indices != null ?
                        View(_indices) :
                        Problem("Entité set 'symbols' est null.");
        }

        public async Task<IActionResult> GetImage(int id)
        {

            byte[] imageData = await GetImageFromDatabase(id); // Remplacez par votre méthode pour récupérer l'image

            if (imageData == null || imageData.Length == 0)
            {
                // Chemin absolu vers l'image par défaut dans wwwroot/images
                var defaultImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "default.png");

                // Vérifiez si le fichier existe pour éviter les erreurs
                if (System.IO.File.Exists(defaultImagePath))
                {
                    return PhysicalFile(defaultImagePath, "image/png");
                }
                else
                {
                    // Si le fichier n'existe pas, retournez un statut ou une autre image
                    return NotFound("Image par défaut introuvable");
                }
            }

            return File(imageData, "image/png"); // Remplacez "image/png" par le type MIME de l'image
        }

        private async Task<byte[]> GetImageFromDatabase(int id)
        {
            Indice indice = await _service.ObtenirSelonId(id);

            // Remplacez par votre logique de récupération
            return await Task.FromResult(indice.imageAnalysis != null ? indice.imageAnalysis : new byte[0]);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
