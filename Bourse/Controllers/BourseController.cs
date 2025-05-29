using Bourse.Interfaces;
using Bourse.Models;
using Bourse.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Bourse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BourseController(ILogger<BourseController> logger, IIndiceService service, IScheduledTaskService scheduledTaskService, IWebHostEnvironment webHostEnvironment) : Controller
    {
        private readonly ILogger<BourseController> _logger = logger;
        private readonly IIndiceService _service = service;
        //private IQueryable<Indice>? _indices;
        private List<IndiceDTO>? _indicesDTO;
        private DateTime _dateHistorique;
        private string _cheminFichier = "date.csv";
        private readonly IScheduledTaskService _scheduledTaskService = scheduledTaskService;
        private readonly HttpClient client = new HttpClient();
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

        [HttpGet("Agenda")]
        public async Task<IActionResult> GetAgenda([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            return Ok(await _service.ObtenirAgenda(start, end));
        }

        // GET: BourseController
        [HttpGet("IndexDTOApi")]
        //[Authorize]
        public async Task<IActionResult> IndexDTOApi(string? filtre, string? exchangeFiltre, string? sortOrder, int page = 1, int pageSize = 50)
        {

            if (string.IsNullOrEmpty(filtre))
            {
                using (var sr = new StreamReader(_cheminFichier))
                {
                    while (!sr.EndOfStream)
                    {
                        var ligne = await sr.ReadLineAsync();
                        if (!string.IsNullOrEmpty(ligne))
                            _dateHistorique = DateTime.Parse(ligne);
                    }
                }

                _indicesDTO = await _service.ObtenirToutDTO();
            }
            else
            {
                _indicesDTO = await _service.ObtenirSelonNameDTO(filtre);
            }

            if (!string.IsNullOrEmpty(exchangeFiltre))
            {
                if (exchangeFiltre == "TOR")
                    _indicesDTO = _indicesDTO.Where(i => i.Exchange == "TOR").ToList();
                else
                    _indicesDTO = _indicesDTO.Where(i => i.Exchange != "TOR").ToList();
            }

            _indicesDTO = sortOrder switch
            {
                "symbol_desc" => _indicesDTO.OrderByDescending(i => i.Symbol).ToList(),
                "price_asc" => _indicesDTO.OrderBy(i => i.RegularMarketPrice).ToList(),
                "price_desc" => _indicesDTO.OrderByDescending(i => i.RegularMarketPrice).ToList(),
                "change_asc" => _indicesDTO.OrderBy(i => i.RegularMarketChange).ToList(),
                "change_desc" => _indicesDTO.OrderByDescending(i => i.RegularMarketChange).ToList(),
                "exercfinanc_asc" => _indicesDTO.OrderBy(i =>
                    i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
                    ? i.DatesExercicesFinancieres.Min(d => d) == DateTime.MinValue ? DateTime.MaxValue : i.DatesExercicesFinancieres.Min(d => d)
                    : DateTime.MaxValue).ToList(), // Les �l�ments sans date et avec DateTime.MinValue seront mis en bas

                "exercfinanc_desc" => _indicesDTO.OrderByDescending(i =>
                    i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
                        ? i.DatesExercicesFinancieres.Min(d => d) == DateTime.MinValue ? DateTime.MinValue : i.DatesExercicesFinancieres.Min(d => d)
                        : DateTime.MinValue).ToList(), // Les �l�ments sans date et avec DateTime.MinValue seront mis en bas

                "bourse_asc" => _indicesDTO.OrderBy(i => i.Exchange).ToList(),
                "bourse_desc" => _indicesDTO.OrderByDescending(i => i.Exchange).ToList(),
                "label_asc" => _indicesDTO.OrderBy(i => i.Label).ToList(),
                "label_desc" => _indicesDTO.OrderByDescending(i => i.Label).ToList(),
                "prob_asc" => _indicesDTO.OrderBy(i => i.Probability).ToList(),
                "prob_desc" => _indicesDTO.OrderByDescending(i => i.Probability).ToList(),
                _ => _indicesDTO.OrderBy(i => i.Symbol).ToList()
            };

            // Pagination
            var paginatedList = await PaginatedList<IndiceDTO>.CreateAsync(_indicesDTO, page, pageSize);

            return Ok(new
            {
                items = paginatedList, // les donn�es pagin�es
                currentPage = paginatedList.PageIndex,
                totalPages = paginatedList.TotalPages,
                pageSize
            });
        }

        // GET: TSXController/ForecastsDTOApi
        [HttpGet("ForecastsDTOApi")]
        //[Authorize]
        public async Task<IActionResult> ForecastsDTOApi(string? filtre, string? exchangeFiltre, string? sortOrder, int page = 1, int pageSize = 50)
        {

            if (string.IsNullOrEmpty(filtre))
            {
                _indicesDTO = await _service.ObtenirToutDTO();
            }
            else
            {
                _indicesDTO = await _service.ObtenirSelonNameDTO(filtre);
            }

            if (!string.IsNullOrEmpty(exchangeFiltre))
            {
                _indicesDTO = exchangeFiltre == "TOR"
                    ? _indicesDTO.Where(i => i.Exchange == "TOR").ToList()
                    : _indicesDTO.Where(i => i.Exchange != "TOR").ToList();
            }

            _indicesDTO = _indicesDTO
                .Where(i =>
                    i.DatesExercicesFinancieres != null &&
                    i.DatesExercicesFinancieres.Length > 0 &&
                    i.DatesExercicesFinancieres.Any(d => d.Date != DateTime.MinValue) &&
                    ((i.Raccomandation == "Strong Buy" || i.Raccomandation == "Buy") && i.Probability > 0.45 ||
                     (i.Raccomandation == "Sell" || i.Raccomandation == "Strong Sell") && i.Probability < 0.55)
                )
                .Select(i => new
                {
                    Indice = i,
                    RecommendationOrder = i.Raccomandation == "Strong Buy" ? 1 :
                         i.Raccomandation == "Buy" ? 2 :
                         i.Raccomandation == "Strong Sell" ? 3 :
                         i.Raccomandation == "Sell" ? 4 :
                         5,
                    FirstDate = i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault()
                })
                .OrderBy(i => i.RecommendationOrder)
                .ThenByDescending(i => i.Indice.Probability)
                .ThenBy(i => i.FirstDate)
                .Select(i => i.Indice)
                .ToList();

            _indicesDTO = sortOrder switch
            {
                "symbol_desc" => _indicesDTO.OrderByDescending(i => i.Symbol).ToList(),
                "price_asc" => _indicesDTO.OrderBy(i => i.RegularMarketPrice).ToList(),
                "price_desc" => _indicesDTO.OrderByDescending(i => i.RegularMarketPrice).ToList(),
                "change_asc" => _indicesDTO.OrderBy(i => i.RegularMarketChange).ToList(),
                "change_desc" => _indicesDTO.OrderByDescending(i => i.RegularMarketChange).ToList(),
                "exercfinanc_asc" => _indicesDTO.OrderBy(i =>
                    i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
                    ? i.DatesExercicesFinancieres.Min(d => d) == DateTime.MinValue ? DateTime.MaxValue : i.DatesExercicesFinancieres.Min(d => d)
                    : DateTime.MaxValue).ToList(), // Les �l�ments sans date et avec DateTime.MinValue seront mis en bas

                "exercfinanc_desc" => _indicesDTO.OrderByDescending(i =>
                    i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
                        ? i.DatesExercicesFinancieres.Max(d => d) == DateTime.MinValue ? DateTime.MaxValue : i.DatesExercicesFinancieres.Max(d => d)
                        : DateTime.MinValue).ToList(), // Les �l�ments sans date et avec DateTime.MinValue seront mis en bas

                "bourse_asc" => _indicesDTO.OrderBy(i => i.Exchange).ToList(),
                "bourse_desc" => _indicesDTO.OrderByDescending(i => i.Exchange).ToList(),
                "label_asc" => _indicesDTO.OrderBy(i => i.Label).ToList(),
                "label_desc" => _indicesDTO.OrderByDescending(i => i.Label).ToList(),
                "prob_asc" => _indicesDTO.OrderBy(i => i.Probability).ToList(),
                "prob_desc" => _indicesDTO.OrderByDescending(i => i.Probability).ToList(),
                _ => _indicesDTO
            };

            // Pagination
            var paginatedList = await PaginatedList<IndiceDTO>.CreateAsync(_indicesDTO, page, pageSize);

            return Ok(new
            {
                items = paginatedList, // les donn�es pagin�es
                currentPage = paginatedList.PageIndex,
                totalPages = paginatedList.TotalPages,
                pageSize
            });
        }


        // GET: TSXController/DetailsDTO/5
        [HttpGet("DetailsDTOApi/{item}")]
        //[Authorize]
        public async Task<IActionResult> DetailsDTOApi(string item, string returnUrl)
        {
            IndiceDTO? indiceDTO = await _service.ObtenirSelonSymbolDTO(item);
            if (indiceDTO == null)
            {
                _logger.LogError($"Une erreur c'est produite lors de la r�cup�ration d'une indice. Symbol = {item}");
                return NotFound(new { message = $"Indice non trouv� pour le symbole {item}" });

            }
            return Ok(indiceDTO);
        }

        //// GET: BourseController
        //[HttpGet("IndexDTO")]
        //public async Task<IActionResult> IndexDTO(string filtre = "", string exchangeFiltre = "", string sortOrder = "", int page = 1, int pageSize = 50, bool resethistorique = false)
        //{
        //    if (resethistorique)
        //    {
        //        using (StreamWriter sw = new StreamWriter(_cheminFichier))
        //        {
        //            sw.WriteLineAsync(DateTime.Now.ToString());
        //        }

        //        //await _service.DeleteHistoriqueRealPrices();
        //    }

        //    ViewBag.Exchanges = new List<dynamic> { new { Key = "NYC", Value = "New York" }, new { Key = "TOR", Value = "Toronto" } };

        //    ViewBag.CurrentSort = sortOrder;

        //    ViewBag.SymbolSortParm = String.IsNullOrEmpty(sortOrder) ? "symbol_desc" : "";
        //    ViewBag.PriceSortParm = sortOrder == "price_desc" ? "price_asc" : "price_desc";
        //    ViewBag.ChangeSortParm = sortOrder == "change_asc" ? "change_desc" : "change_asc";
        //    ViewBag.DatesExercFinancParm = sortOrder == "exercfinanc_asc" ? "exercfinanc_desc" : "exercfinanc_asc";
        //    ViewBag.BourseParm = sortOrder == "bourse_asc" ? "bourse_desc" : "bourse_asc";
        //    ViewBag.LabelSortParm = sortOrder == "label_asc" ? "label_desc" : "label_asc";
        //    ViewBag.ProbSortParm = sortOrder == "prob_desc" ? "prob_asc" : "prob_desc";

        //    if (filtre is null)
        //    {
        //        using (StreamReader sr = new StreamReader(_cheminFichier))
        //        {
        //            while (!sr.EndOfStream)
        //            {
        //                string ligne = await sr.ReadLineAsync();
        //                if (!string.IsNullOrEmpty(ligne))
        //                {
        //                    //while (ligne != null)
        //                    //{
        //                    _dateHistorique = DateTime.Parse(ligne);
        //                    //}
        //                }
        //            }
        //        }

        //        _indicesDTO = await _service.ObtenirToutDTO();

        //    }
        //    else
        //    {
        //        ViewData["actifFiltre"] = filtre;
        //        _indicesDTO = await _service.ObtenirSelonNameDTO(filtre);
        //    }

        //    // Stocker le filtre actif
        //    ViewData["ExchangeFiltre"] = exchangeFiltre;

        //    if (!string.IsNullOrEmpty(exchangeFiltre))
        //    {
        //        if (exchangeFiltre == "TOR")
        //        {
        //            _indicesDTO = _indicesDTO.Where(i => i.Exchange == exchangeFiltre);
        //        }
        //        else
        //        {
        //            _indicesDTO = _indicesDTO.Where(i => i.Exchange != "TOR");
        //        }

        //    }

        //    ViewData["DateReset"] = _dateHistorique.ToString("yyyy-MM-dd");

        //    switch (sortOrder)
        //    {
        //        case "symbol_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.Symbol);
        //            break;
        //        case "price_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.RegularMarketPrice);
        //            break;
        //        case "price_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.RegularMarketPrice);
        //            break;
        //        case "change_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.RegularMarketChange);
        //            break;
        //        case "change_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.RegularMarketChange);
        //            break;
        //        case "exercfinanc_asc":
        //            _indicesDTO = _indicesDTO
        //                .OrderBy(i => i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
        //                    ? i.DatesExercicesFinancieres.Min(d => d)
        //                    : DateTime.MaxValue); // les sans dates vont tout en bas
        //            break;

        //        case "exercfinanc_desc":
        //            _indicesDTO = _indicesDTO
        //                .OrderByDescending(i => i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
        //                    ? i.DatesExercicesFinancieres.Min(d => d)
        //                    : DateTime.MinValue); // les sans dates vont tout en bas en descendant
        //            break;
        //        case "bourse_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.Exchange);
        //            break;
        //        case "bourse_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.Exchange);
        //            break;
        //        case "label_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.Label);
        //            break;
        //        case "label_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.Label);
        //            break;
        //        case "prob_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.Probability);
        //            break;
        //        case "prob_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.Probability);
        //            break;
        //        default:
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.Symbol);  // Par d�faut, tri croissant sur le prix
        //            break;
        //    }

        //    // Pagination
        //    var paginatedList = await PaginatedList<IndiceDTO>.CreateAsync(_indicesDTO, page, pageSize);

        //    ViewData["CurrentPage"] = page;
        //    ViewData["TotalPages"] = paginatedList.TotalPages;
        //    ViewData["PageSize"] = pageSize;

        //    return paginatedList != null ?
        //                View(paginatedList) :
        //                Problem("Entit� set 'symbols' est null.");
        //}

        //// GET: TSXController/DetailsDTO/5
        //[HttpGet("Bourse/DetailsDTO/{item}")]
        //public async Task<IActionResult> DetailsDTO(string item, string returnUrl, bool resethistorique = false)
        //{
        //    ViewData["ReturnUrl"] = returnUrl;

        //    IndiceDTO indiceDTO = await _service.ObtenirSelonSymbolDTO(item);
        //    if (indiceDTO == null)
        //    {
        //        _logger.LogError($"Une erreur c'est produite lors de la r�cup�ration d'une indice. Symbol = {item}");
        //        return NotFound();

        //    }
        //    return View(indiceDTO);
        //}

        //// GET: TSXController/ForecastsDTO
        //[HttpGet("Bourse/ForecastsDTO")]
        //public async Task<IActionResult> ForecastsDTO(string filtre, string exchangeFiltre, string sortOrder, int page = 1, int pageSize = 50, bool resethistorique = false)
        //{
        //    if (resethistorique)
        //    {
        //        using (StreamWriter sw = new StreamWriter(_cheminFichier))
        //        {
        //            sw.WriteLineAsync(DateTime.Now.ToString());
        //        }

        //        //await _service.DeleteHistoriqueRealPrices();
        //    }

        //    ViewBag.Exchanges = new List<dynamic> { new { Key = "NYC", Value = "New York" }, new { Key = "TOR", Value = "Toronto" } };

        //    ViewBag.CurrentSort = sortOrder;

        //    ViewBag.SymbolSortParm = String.IsNullOrEmpty(sortOrder) ? "symbol_desc" : "";
        //    ViewBag.PriceSortParm = sortOrder == "price_desc" ? "price_asc" : "price_desc";
        //    ViewBag.ChangeSortParm = sortOrder == "change_asc" ? "change_desc" : "change_asc";
        //    ViewBag.DatesExercFinancParm = sortOrder == "exercfinanc_asc" ? "exercfinanc_desc" : "exercfinanc_asc";
        //    ViewBag.BourseParm = sortOrder == "bourse_asc" ? "bourse_desc" : "bourse_asc";
        //    ViewBag.LabelSortParm = sortOrder == "label_asc" ? "label_desc" : "label_asc";
        //    ViewBag.ProbSortParm = sortOrder == "prob_desc" ? "prob_asc" : "prob_desc";

        //    if (filtre is null)
        //    {
        //        using (StreamReader sr = new StreamReader(_cheminFichier))
        //        {
        //            while (!sr.EndOfStream)
        //            {
        //                string ligne = await sr.ReadLineAsync();
        //                if (!string.IsNullOrEmpty(ligne))
        //                {
        //                    //while (ligne != null)
        //                    //{
        //                    _dateHistorique = DateTime.Parse(ligne);
        //                    //}
        //                }
        //            }
        //        }

        //        _indicesDTO = await _service.ObtenirToutDTO();

        //    }
        //    else
        //    {
        //        ViewData["actifFiltre"] = filtre;
        //        _indicesDTO = await _service.ObtenirSelonNameDTO(filtre);
        //    }

        //    // Stocker le filtre actif
        //    ViewData["ExchangeFiltre"] = exchangeFiltre;

        //    if (!string.IsNullOrEmpty(exchangeFiltre))
        //    {
        //        if (exchangeFiltre == "TOR")
        //        {
        //            _indicesDTO = _indicesDTO.Where(i => i.Exchange == exchangeFiltre);
        //        }
        //        else
        //        {
        //            _indicesDTO = _indicesDTO.Where(i => i.Exchange != "TOR");
        //        }

        //    }

        //    //// D�finir l'ordre des recommandations
        //    //var recommendationOrder = new Dictionary<string, int>
        //    //{
        //    //    { "Strong Buy", 1 },
        //    //    { "Buy", 2 },
        //    //    { "Strong Sell", 3 },
        //    //    { "Sell", 4 }
        //    //};

        //    _indicesDTO = _indicesDTO
        //        .Where(i =>
        //            //(i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() > DateTime.Now &&
        //            //i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() < DateTime.Now.AddDays(90) &&
        //            //i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() != DateTime.MinValue) &&
        //            //i.QuoteType != "ETF" &&
        //            i.DatesExercicesFinancieres.Any(d => d.Date != DateTime.MinValue) &&
        //            (i.DatesExercicesFinancieres != null || i.DatesExercicesFinancieres.Length > 0) &&
        //            //(i.Raccomandation == "Strong Buy" || i.Raccomandation == "Buy" ||
        //            //i.Raccomandation == "Sell" || i.Raccomandation == "Strong Sell" ||
        //            ((i.Raccomandation == "Strong Buy" || i.Raccomandation == "Buy") && (i.Probability > 0.45 && i.Probability != 0) ||
        //            (i.Raccomandation == "Sell" || i.Raccomandation == "Strong Sell") && (i.Probability < 0.55 && i.Probability != 0) 
        //            //|| (i.Raccomandation == "Hold" && (i.Probability > 0.75 || i.Probability < 0.20) && i.Probability != 0)
        //            )
        //        )
        //        .Select(i => new
        //        {
        //            Indice = i,
        //            RecommendationOrder = i.Raccomandation == "Strong Buy" ? 1 :
        //                 i.Raccomandation == "Buy" ? 2 :
        //                 i.Raccomandation == "Strong Sell" ? 3 :
        //                 i.Raccomandation == "Sell" ? 4 :
        //                 5,
        //            FirstDate = i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault()
        //        })
        //        .OrderBy(i => i.RecommendationOrder) // Trier par recommandation
        //        .ThenByDescending(i => i.Indice.Probability)
        //        .ThenBy(i => i.FirstDate)
        //        .Select(i => i.Indice);

        //    //_indicesDTO = _indicesDTO
        //    //    .Where(i =>
        //    //        //(i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() > DateTime.Now &&
        //    //        //i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() < DateTime.Now.AddDays(90) &&
        //    //        //i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() != DateTime.MinValue) &&
        //    //        //i.QuoteType != "ETF" &&
        //    //        i.DatesExercicesFinancieres.Any(d => d.Date != DateTime.MinValue) &&
        //    //        (i.DatesExercicesFinancieres != null || i.DatesExercicesFinancieres.Length > 0) &&
        //    //        ((i.Raccomandation == "Strong Buy" || i.Raccomandation == "Buy") ||
        //    //        (i.Raccomandation == "Sell" || i.Raccomandation == "Strong Sell"))
        //    //    )
        //    //    .OrderByDescending(i => i.Probability)  // Tri par Probability d�croissant
        //    //    .ThenBy(i => i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault()); // Tri par la premi�re date croissante

        //    ViewData["DateReset"] = _dateHistorique.ToString("yyyy-MM-dd");

        //    switch (sortOrder)
        //    {
        //        case "symbol_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.Symbol);
        //            break;
        //        case "price_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.RegularMarketPrice);
        //            break;
        //        case "price_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.RegularMarketPrice);
        //            break;
        //        case "change_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.RegularMarketChange);
        //            break;
        //        case "change_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.RegularMarketChange);
        //            break;
        //        case "exercfinanc_asc":
        //            _indicesDTO = _indicesDTO
        //                .OrderBy(i => i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
        //                    ? i.DatesExercicesFinancieres.Min(d => d)
        //                    : DateTime.MaxValue); // les sans dates vont tout en bas
        //            break;

        //        case "exercfinanc_desc":
        //            _indicesDTO = _indicesDTO
        //                .OrderByDescending(i => i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
        //                    ? i.DatesExercicesFinancieres.Min(d => d)
        //                    : DateTime.MinValue); // les sans dates vont tout en bas en descendant
        //            break;
        //        case "bourse_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.Exchange);
        //            break;
        //        case "bourse_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.Exchange);
        //            break;
        //        case "label_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.Label);
        //            break;
        //        case "label_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.Label);
        //            break;
        //        case "prob_asc":
        //            _indicesDTO = _indicesDTO.OrderBy(i => i.Probability);
        //            break;
        //        case "prob_desc":
        //            _indicesDTO = _indicesDTO.OrderByDescending(i => i.Probability);
        //            break;
        //    }

        //    // Pagination
        //    var paginatedList = await PaginatedList<IndiceDTO>.CreateAsync(_indicesDTO, page, pageSize);

        //    ViewData["CurrentPage"] = page;
        //    ViewData["TotalPages"] = paginatedList.TotalPages;
        //    ViewData["PageSize"] = pageSize;

        //    return paginatedList != null ?
        //                View(paginatedList) :
        //                Problem("Entit� set 'symbols' est null.");
        //}

        //// GET: TSXController
        //public async Task<IActionResult> Index(string filtre, string exchangeFiltre, string sortOrder, int page = 1, int pageSize = 50, bool resethistorique = false)
        //{
        //    if (resethistorique)
        //    {
        //        using (StreamWriter sw = new StreamWriter(_cheminFichier))
        //        {
        //            sw.WriteLineAsync(DateTime.Now.ToString());
        //        }

        //        //await _service.DeleteHistoriqueRealPrices();
        //    }

        //    ViewBag.Exchanges = new List<dynamic> { new { Key = "NYC", Value = "New York" }, new { Key = "TOR", Value = "Toronto" } };

        //    ViewBag.CurrentSort = sortOrder;

        //    ViewBag.SymbolSortParm = String.IsNullOrEmpty(sortOrder) ? "symbol_desc" : "";
        //    ViewBag.PriceSortParm = sortOrder == "price_desc" ? "price_asc" : "price_desc";
        //    ViewBag.ChangeSortParm = sortOrder == "change_asc" ? "change_desc" : "change_asc";
        //    ViewBag.DatesExercFinancParm = sortOrder == "exercfinanc_asc" ? "exercfinanc_desc" : "exercfinanc_asc";
        //    ViewBag.BourseParm = sortOrder == "bourse_asc" ? "bourse_desc" : "bourse_asc";
        //    ViewBag.LabelSortParm = sortOrder == "label_asc" ? "label_desc" : "label_asc";
        //    ViewBag.ProbSortParm = sortOrder == "prob_desc" ? "prob_asc" : "prob_desc";

        //    if (filtre is null)
        //    {
        //        using (StreamReader sr = new StreamReader(_cheminFichier))
        //        {
        //            while (!sr.EndOfStream)
        //            {
        //                string ligne = await sr.ReadLineAsync();
        //                if (!string.IsNullOrEmpty(ligne))
        //                {
        //                    //while (ligne != null)
        //                    //{
        //                    _dateHistorique = DateTime.Parse(ligne);
        //                    //}
        //                }
        //            }
        //        }

        //        _indices = await _service.ObtenirTout();

        //    }
        //    else
        //    {
        //        ViewData["actifFiltre"] = filtre;
        //        _indices = await _service.ObtenirSelonName(filtre);
        //    }

        //    // Stocker le filtre actif
        //    ViewData["ExchangeFiltre"] = exchangeFiltre;

        //    if (!string.IsNullOrEmpty(exchangeFiltre))
        //    {
        //        if (exchangeFiltre == "TOR")
        //        {
        //            _indices = _indices.Where(i => i.Exchange == exchangeFiltre);
        //        }
        //        else
        //        {
        //            _indices = _indices.Where(i => i.Exchange != "TOR");
        //        }

        //    }

        //    ViewData["DateReset"] = _dateHistorique.ToString("yyyy-MM-dd");

        //    switch (sortOrder)
        //    {
        //        case "symbol_desc":
        //            _indices = _indices.OrderByDescending(i => i.Symbol);
        //            break;
        //        case "price_asc":
        //            _indices = _indices.OrderBy(i => i.RegularMarketPrice);
        //            break;
        //        case "price_desc":
        //            _indices = _indices.OrderByDescending(i => i.RegularMarketPrice);
        //            break;
        //        case "change_asc":
        //            _indices = _indices.OrderBy(i => i.RegularMarketChange);
        //            break;
        //        case "change_desc":
        //            _indices = _indices.OrderByDescending(i => i.RegularMarketChange);
        //            break;
        //        case "exercfinanc_asc":
        //            _indices = _indices
        //                .OrderBy(i => i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
        //                    ? i.DatesExercicesFinancieres.Min(d => d)
        //                    : DateTime.MaxValue); // les sans dates vont tout en bas
        //            break;

        //        case "exercfinanc_desc":
        //            _indices = _indices
        //                .OrderByDescending(i => i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
        //                    ? i.DatesExercicesFinancieres.Min(d => d)
        //                    : DateTime.MinValue); // les sans dates vont tout en bas en descendant
        //            break;
        //        case "bourse_asc":
        //            _indices = _indices.OrderBy(i => i.Exchange);
        //            break;
        //        case "bourse_desc":
        //            _indices = _indices.OrderByDescending(i => i.Exchange);
        //            break;
        //        case "label_asc":
        //            _indices = _indices.OrderBy(i => i.Label);
        //            break;
        //        case "label_desc":
        //            _indices = _indices.OrderByDescending(i => i.Label);
        //            break;
        //        case "action_asc":
        //            _indices = _indices.OrderBy(i => i.IsIncreasing);
        //            break;
        //        case "action_desc":
        //            _indices = _indices.OrderByDescending(i => i.IsIncreasing);
        //            break;
        //        case "prob_asc":
        //            _indices = _indices.OrderBy(i => i.Probability);
        //            break;
        //        case "prob_desc":
        //            _indices = _indices.OrderByDescending(i => i.Probability);
        //            break;
        //        default:
        //            _indices = _indices.OrderBy(i => i.Symbol);
        //            //_indices = _indices.OrderBy(i => i.Symbol).ToList();  // Par d�faut, tri croissant sur le prix
        //            break;
        //    }

        //    // Pagination
        //    var paginatedList = await PaginatedList<Indice>.CreateAsync(_indices, page, pageSize);

        //    ViewData["CurrentPage"] = page;
        //    ViewData["TotalPages"] = paginatedList.TotalPages;
        //    ViewData["PageSize"] = pageSize;

        //    return paginatedList != null ?
        //                View(paginatedList) :
        //                Problem("Entit� set 'symbols' est null.");
        //}

        //// GET: TSXController/Details/5
        //[HttpGet("Bourse/Details/{item}")]
        //public async Task<IActionResult> Details(string item, string returnUrl, bool resethistorique = false)
        //{
        //    ViewData["ReturnUrl"] = returnUrl;

        //    Indice indice = await _service.ObtenirSelonSymbol(item);
        //    if (indice == null)
        //    {
        //        _logger.LogError($"Une erreur c'est produite lors de la r�cup�ration d'une indice. Symbol = {item}");
        //        return NotFound();

        //    }
        //    return View(indice);
        //}

        //// GET: TSXController/Forecasts
        //[HttpGet("Bourse/Forecasts")]
        //public async Task<IActionResult> Forecasts(string filtre, string exchangeFiltre, string sortOrder, int page = 1, int pageSize = 50, bool resethistorique = false)
        //{
        //    if (resethistorique)
        //    {
        //        using (StreamWriter sw = new StreamWriter(_cheminFichier))
        //        {
        //            sw.WriteLineAsync(DateTime.Now.ToString());
        //        }

        //        //await _service.DeleteHistoriqueRealPrices();
        //    }

        //    ViewBag.Exchanges = new List<dynamic> { new { Key = "NYC", Value = "New York" }, new { Key = "TOR", Value = "Toronto" } };

        //    ViewBag.CurrentSort = sortOrder;

        //    ViewBag.SymbolSortParm = String.IsNullOrEmpty(sortOrder) ? "symbol_desc" : "";
        //    ViewBag.PriceSortParm = sortOrder == "price_desc" ? "price_asc" : "price_desc";
        //    ViewBag.ChangeSortParm = sortOrder == "change_asc" ? "change_desc" : "change_asc";
        //    ViewBag.DatesExercFinancParm = sortOrder == "exercfinanc_asc" ? "exercfinanc_desc" : "exercfinanc_asc";
        //    ViewBag.BourseParm = sortOrder == "bourse_asc" ? "bourse_desc" : "bourse_asc";
        //    ViewBag.LabelSortParm = sortOrder == "label_asc" ? "label_desc" : "label_asc";
        //    ViewBag.ProbSortParm = sortOrder == "prob_desc" ? "prob_asc" : "prob_desc";

        //    if (filtre is null)
        //    {
        //        using (StreamReader sr = new StreamReader(_cheminFichier))
        //        {
        //            while (!sr.EndOfStream)
        //            {
        //                string ligne = await sr.ReadLineAsync();
        //                if (!string.IsNullOrEmpty(ligne))
        //                {
        //                    //while (ligne != null)
        //                    //{
        //                    _dateHistorique = DateTime.Parse(ligne);
        //                    //}
        //                }
        //            }
        //        }

        //        _indices = await _service.ObtenirTout();

        //    }
        //    else
        //    {
        //        ViewData["actifFiltre"] = filtre;
        //        _indices = await _service.ObtenirSelonName(filtre);
        //    }

        //    // Stocker le filtre actif
        //    ViewData["ExchangeFiltre"] = exchangeFiltre;

        //    if (!string.IsNullOrEmpty(exchangeFiltre))
        //    {
        //        if (exchangeFiltre == "TOR")
        //        {
        //            _indices = _indices.Where(i => i.Exchange == exchangeFiltre);
        //        }
        //        else
        //        {
        //            _indices = _indices.Where(i => i.Exchange != "TOR");
        //        }

        //    }

        //    //// D�finir l'ordre des recommandations
        //    //var recommendationOrder = new Dictionary<string, int>
        //    //{
        //    //    { "Strong Buy", 1 },
        //    //    { "Buy", 2 },
        //    //    { "Strong Sell", 3 },
        //    //    { "Sell", 4 }
        //    //};

        //    _indices = _indices
        //        .Where(i =>
        //            //(i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() > DateTime.Now &&
        //            //i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() < DateTime.Now.AddDays(90) &&
        //            //i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault() != DateTime.MinValue) &&
        //            //i.QuoteType != "ETF" &&
        //            i.DatesExercicesFinancieres.Any(d => d.Date != DateTime.MinValue) &&
        //            (i.DatesExercicesFinancieres != null || i.DatesExercicesFinancieres.Length > 0) &&
        //            //(i.Raccomandation == "Strong Buy" || i.Raccomandation == "Buy" ||
        //            //i.Raccomandation == "Sell" || i.Raccomandation == "Strong Sell" ||
        //            ((i.Raccomandation == "Strong Buy" || i.Raccomandation == "Buy") && (i.Probability > 0.45 && i.Probability != 0) ||
        //            (i.Raccomandation == "Sell" || i.Raccomandation == "Strong Sell") && (i.Probability < 0.55 && i.Probability != 0)
        //            //|| (i.Raccomandation == "Hold" && (i.Probability > 0.75 || i.Probability < 0.20) && i.Probability != 0)
        //            )
        //        )
        //        .Select(i => new
        //        {
        //            Indice = i,
        //            RecommendationOrder = i.Raccomandation == "Strong Buy" ? 1 :
        //                 i.Raccomandation == "Buy" ? 2 :
        //                 i.Raccomandation == "Strong Sell" ? 3 :
        //                 i.Raccomandation == "Sell" ? 4 :
        //                 5,
        //            FirstDate = i.DatesExercicesFinancieres.OrderBy(d => d.Date).FirstOrDefault()
        //        })
        //        .OrderBy(i => i.RecommendationOrder) // Trier par recommandation
        //        .ThenByDescending(i => i.Indice.Probability)
        //        .ThenBy(i => i.FirstDate)
        //        .Select(i => i.Indice);

        //    ViewData["DateReset"] = _dateHistorique.ToString("yyyy-MM-dd");

        //    switch (sortOrder)
        //    {
        //        case "symbol_desc":
        //            _indices = _indices.OrderByDescending(i => i.Symbol);
        //            break;
        //        case "price_asc":
        //            _indices = _indices.OrderBy(i => i.RegularMarketPrice);
        //            break;
        //        case "price_desc":
        //            _indices = _indices.OrderByDescending(i => i.RegularMarketPrice);
        //            break;
        //        case "change_asc":
        //            _indices = _indices.OrderBy(i => i.RegularMarketChange);
        //            break;
        //        case "change_desc":
        //            _indices = _indices.OrderByDescending(i => i.RegularMarketChange);
        //            break;
        //        case "exercfinanc_asc":
        //            _indices = _indices
        //                .OrderBy(i => i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
        //                    ? i.DatesExercicesFinancieres.Min(d => d)
        //                    : DateTime.MaxValue); // les sans dates vont tout en bas
        //            break;

        //        case "exercfinanc_desc":
        //            _indices = _indices
        //                .OrderByDescending(i => i.DatesExercicesFinancieres != null && i.DatesExercicesFinancieres.Any()
        //                    ? i.DatesExercicesFinancieres.Min(d => d)
        //                    : DateTime.MinValue); // les sans dates vont tout en bas en descendant
        //            break;
        //        case "bourse_asc":
        //            _indices = _indices.OrderBy(i => i.Exchange);
        //            break;
        //        case "bourse_desc":
        //            _indices = _indices.OrderByDescending(i => i.Exchange);
        //            break;
        //        case "label_asc":
        //            _indices = _indices.OrderBy(i => i.Label);
        //            break;
        //        case "label_desc":
        //            _indices = _indices.OrderByDescending(i => i.Label);
        //            break;
        //        case "action_asc":
        //            _indices = _indices.OrderBy(i => i.IsIncreasing);
        //            break;
        //        case "action_desc":
        //            _indices = _indices.OrderByDescending(i => i.IsIncreasing);
        //            break;
        //        case "prob_asc":
        //            _indices = _indices.OrderBy(i => i.Probability);
        //            break;
        //        case "prob_desc":
        //            _indices = _indices.OrderByDescending(i => i.Probability);
        //            break;
        //            //default:
        //            //    _indices = _indices.OrderBy(i => i.Symbol).ToList();  // Par d�faut, tri croissant sur le prix
        //            //    break;
        //    }

        //    // Pagination
        //    var paginatedList = await PaginatedList<Indice>.CreateAsync(_indices, page, pageSize);

        //    ViewData["CurrentPage"] = page;
        //    ViewData["TotalPages"] = paginatedList.TotalPages;
        //    ViewData["PageSize"] = pageSize;

        //    return paginatedList != null ?
        //                View(paginatedList) :
        //                Problem("Entit� set 'symbols' est null.");
        //}

        //public async Task<IActionResult> GetImage(int id)
        //{

        //    byte[] imageData = await GetImageFromDatabase(id); // Remplacez par votre m�thode pour r�cup�rer l'image

        //    if (imageData == null || imageData.Length == 0)
        //    {
        //        // Chemin absolu vers l'image par d�faut dans wwwroot/images
        //        var defaultImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "default.png");

        //        // V�rifiez si le fichier existe pour �viter les erreurs
        //        if (System.IO.File.Exists(defaultImagePath))
        //        {
        //            return PhysicalFile(defaultImagePath, "image/png");
        //        }
        //        else
        //        {
        //            // Si le fichier n'existe pas, retournez un statut ou une autre image
        //            return NotFound("Image par d�faut introuvable");
        //        }
        //    }

        //    return File(imageData, "image/png"); // Remplacez "image/png" par le type MIME de l'image
        //}

        //private async Task<byte[]> GetImageFromDatabase(int id)
        //{
        //    Indice indice = await _service.ObtenirSelonId(id);

        //    // Remplacez par votre logique de r�cup�ration
        //    return await Task.FromResult(indice.imageAnalysis != null ? indice.imageAnalysis : new byte[0]);
        //}

        //public IActionResult Privacy()
        //{
        //    return View();
        //}

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}
