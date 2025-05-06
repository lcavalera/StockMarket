import axios from 'axios';
import type { UserProfile } from '../Data/typeusers';

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
    volume: number;
    label: boolean;
    probability: number;
    raccomandation: string;
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

interface LoginModel {
  userName: string;
  password: string;
}

// const API_BASE_URL = 'http://localhost:5184/'; // Remplacez par l'URL de votre API
// const API_BASE_URL = 'https://localhost:7157/'; // Remplacez par l'URL de votre API
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7157/'; // Valeur par défaut si la variable n'est pas définie
// const IDENTITY_BASE_URL = 'http://localhost:5235/'; // Remplacez par l'URL de votre IDENTITY SERVER
// const IDENTITY_BASE_URL = 'https://localhost:7248/'; // Remplacez par l'URL de votre IDENTITY SERVER
const IDENTITY_BASE_URL = process.env.REACT_APP_IDENTITY_BASE_URL || 'https://localhost:7248/'; // Valeur par défaut si la variable n'est pas définie

const httpclientApi = axios.create({
  baseURL: API_BASE_URL,
  timeout: 50000
});

const httpclientIdentity = axios.create({
  baseURL: IDENTITY_BASE_URL,
  timeout: 50000
});

// Ajouter un interceptor pour inclure automatiquement le token JWT
httpclientApi.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem('token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);


httpclientApi.defaults.headers.post['Content-Type'] = 'application/json';

// Déclaration du type de la réponse
interface PaginatedResponse {
    items: IndiceDTO[];
    totalPages: number;
  }

  // Fonction pour récupérer l'Agenda
  export const getAgendaSemaine = async (): Promise<IndiceDTO[]> => {
    try {
      const today = new Date();
      const start = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1); // minuit
      const end = new Date(start);
      end.setDate(end.getDate() + 6);

      const startStr = start.toISOString();
      const endStr = end.toISOString();

      const response = await httpclientApi.get<IndiceDTO[]>(
        `Bourse/agenda?start=${startStr}&end=${endStr}`
      );
  
      return response.data;
    } catch (error) {
      console.error("Erreur lors du chargement de l'agenda :", error);
      throw new Error("Impossible de charger l’agenda de la semaine.");
    }
  };

  
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
    if (filtre) params.append('filtre', filtre);
    if (exchangeFiltre) params.append('exchangeFiltre', exchangeFiltre);
    if (sortOrder) params.append('sortOrder', sortOrder);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    // Indication explicite du type de réponse attendue
    const response = await httpclientApi.get<PaginatedResponse>(`Bourse/IndexDTOApi?${params.toString()}`);
    
    return response.data; // TypeScript sait maintenant que c'est de type PaginatedResponse
  } catch (error) {
    console.error('Erreur de chargement des indices:', error);
    throw new Error('Erreur de chargement des indices');
  }
};

// Fonction pour récupérer les indices avec les filtres, tri, et pagination
export const getForecasts = async (
  filtre: string,
  exchangeFiltre: string,
  sortOrder: string,
  page: number = 1,
  pageSize: number = 50
): Promise<PaginatedResponse> => { // Précise le type de la réponse retournée
  try {
    const params = new URLSearchParams();
    if (filtre) params.append('filtre', filtre);
    if (exchangeFiltre) params.append('exchangeFiltre', exchangeFiltre);
    if (sortOrder) params.append('sortOrder', sortOrder);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    console.log(params);
    // Indication explicite du type de réponse attendue
    const response = await httpclientApi.get<PaginatedResponse>(`Bourse/ForecastsDTOApi?${params.toString()}`);
    return response.data; // TypeScript sait maintenant que c'est de type PaginatedResponse
  } catch (error) {
    console.error('Erreur de chargement des indices:', error);
    throw new Error('Erreur de chargement des indices');
  }
};

// Fonction pour récupérer les détails d'un indice spécifique
export const getIndiceDetails = async (symbol: string, returnUrl: string = "list"): Promise<IndiceDTO> => {
  try {
    const response = await httpclientApi.get<IndiceDTO>(`Bourse/DetailsDTOApi/${symbol}?returnUrl=${returnUrl}`); // Remplace `/api/indices/${symbol}` par l'URL de ton API

    return response.data; // Retourne l'objet IndiceDTO
  } catch (error) {
    console.error('Erreur dans la récupération des détails de l\'indice:', error);
    throw error; // Lance l'erreur pour gestion ultérieure
  }
};

export const refreshHeaderLogin = () => {
  const event = new Event('refresh-login');
  window.dispatchEvent(event);
};

export const login = async (credentials: LoginModel): Promise<{ token: string; user: UserProfile }> => {
  try {
    const response = await httpclientIdentity.post<{ token: string; user: UserProfile }>('auth/login', credentials);
    const { token, user } = response.data;

    sessionStorage.setItem('token', token);
    sessionStorage.setItem('user', JSON.stringify(user)); // <<< on ajoute ça

    return { token, user };
  } catch (error) {
    console.error('Erreur de login:', error);
    throw new Error('Erreur de login');
  }
};


// Fonction d'enregistrement dans dataApi.ts
export const registerUser = async (formData: any) => {
  try {
    console.log("Envoi des données d'inscription : ", formData);
    const response = await httpclientIdentity.post('auth/register', formData); // L'URL d'enregistrement

    console.log("Réponse de l'API : ", response);

    // Vérifie si la réponse est celle attendue (création réussie)
    if (response.status === 200 || response.status === 201) {
      return { success: true, message: 'Utilisateur créé avec succès' };
    }

    // Si le statut n'est ni 200 ni 201, c'est une erreur
    throw new Error('Erreur lors de la création de l\'utilisateur');
  } catch (error: any) {
    // Affiche les erreurs détaillées si disponibles
    if (error.response) {
      console.error("Erreur de la réponse : ", error.response);
      return { success: false, message: error.response.data.message || 'Erreur lors de l\'enregistrement, veuillez réessayer.' };
    } else if (error.request) {
      console.error("Aucune réponse du serveur : ", error.request);
      return { success: false, message: 'Erreur de communication avec le serveur' };
    } else {
      console.error("Erreur inconnue : ", error.message);
      return { success: false, message: 'Erreur lors de l\'enregistrement, veuillez réessayer.' };
    }
  }
};

export const createPayPalOrder = async (description: string, amount: string): Promise<{ orderId: string }> => {
  try {
    const response = await httpclientApi.post<{ orderId: string }>('Payment/create-order', {
      description,
      amount
    });
    return response.data;
  } catch (error) {
    console.error("Erreur lors de la création de l'ordre PayPal :", error);
    throw new Error("Impossible de créer l'ordre PayPal.");
  }
};

