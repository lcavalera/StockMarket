import axios from 'axios';

interface StockData {
    id: number;
    indiceId: number;
    currentPrice: number;
    prevPrice: number;
    open: number;
    high: number;
    low: number;
    date: Date;
    isIncreasing: boolean;
    change: number;
    changePercent: number;
    sma14: number;
    sma14Display: string;
    rsi14: number;
    rsi14Display: string;
    futurePrice: number;
    ema14: number;
    bollingerUpper: number;
    bollingerLower: number;
    macd: number;
    averageVolume: number;
}

interface IndiceDTO {
    id: number;
    symbol: string;
    name: string;
    regularMarketPrice: number;
    regularMarketChange: number;
    regularMarketOpen: number;
    regularMarketPreviousClose: number;
    regularMarketDayHigh: number;
    regularMarketDayLow: number;
    regularMarketChangePercent: number;
    regularMarketVolume: number;
    quoteType: string;
    exchange: string;
    exchangeTimezoneName: string;
    exchangeTimezoneShortName: string;
    bourse: string;
    datesExercicesFinancieres: Date[];
    label: boolean;
    isIncreasing: boolean;
    probability: number;
    raccomandation: string;
    dateUpdated: Date;
    datePrevision: Date;
    trainingData: StockData[];
}

const API_BASE_URL = 'http://localhost:5184/'; // Remplacez par l'URL de votre API

const httpclient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000
});

httpclient.defaults.headers.post['Content-Type'] = 'application/json';

// Déclaration du type de la réponse
interface PaginatedResponse {
    items: IndiceDTO[];
    totalPages: number;
  }

// Fonction pour récupérer les indices avec les filtres, tri, et pagination
export const getIndices = async (
  filtre: string,
  exchangeFiltre: string,
  sortOrder: string,
  page: number = 1,
  pageSize: number = 50
): Promise<PaginatedResponse> => { // Précise le type de la réponse retournée
  try {
    const params = new URLSearchParams();
    // params.append('filtre', filtre);
    // params.append('exchangeFiltre', exchangeFiltre);
    // params.append('sortOrder', sortOrder);
    if (filtre) params.append('filtre', filtre);
    if (exchangeFiltre) params.append('exchangeFiltre', exchangeFiltre);
    if (sortOrder) params.append('sortOrder', sortOrder);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    // Indication explicite du type de réponse attendue
    const response = await httpclient.get<PaginatedResponse>(`IndexDTOApi?${params.toString()}`);
    
    // const response = await httpclient.get<PaginatedResponse>(`IndexDTO?filtre="${filtre}"&exchangeFiltre="${exchangeFiltre}"&sortOrder="${exchangeFiltre}"&page=${page}&pageSize=${pageSize}`);
    return response.data; // TypeScript sait maintenant que c'est de type PaginatedResponse
  } catch (error) {
    console.error('Erreur de chargement des indices:', error);
    throw new Error('Erreur de chargement des indices');
  }
};

// Fonction pour récupérer les détails d'un indice spécifique
export const getIndiceDetails = async (symbol: string, returnUrl: string = "list"): Promise<IndiceDTO> => {
  try {
    const response = await httpclient.get<IndiceDTO>(`DetailsDTOApi/${symbol}?returnUrl=${returnUrl}`); // Remplace `/api/indices/${symbol}` par l'URL de ton API
    console.log("Réponse API détails :", response.data);
    // if (!response.ok) {
    //   throw new Error('Erreur lors de la récupération des données de l\'indice');
    // }
    // const data: IndiceDTO = await response.json();
    return response.data; // Retourne l'objet IndiceDTO
  } catch (error) {
    console.error('Erreur dans la récupération des détails de l\'indice:', error);
    throw error; // Lance l'erreur pour gestion ultérieure
  }
};