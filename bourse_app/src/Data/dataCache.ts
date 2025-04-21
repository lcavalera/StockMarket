import { saveAgendaData, getAgendaData } from '../Data/indexedDB.ts';

const today = new Date().toISOString().split('T')[0];

// Clés utilisées dans IndexedDB
const KEYS = {
  AGENDA: 'agendaData',
  LIST: 'listData',
  FORECASTS: 'forecastsData'
};

// --------- AGENDA ---------

export async function saveAgenda(items: any[]) {
  await saveAgendaData(KEYS.AGENDA, { date: today, data: items });
}

const CACHE_EXPIRY_DAYS = 1; // Par exemple 1 jour

export async function getCachedAgenda() {
  const cached = await getAgendaData(KEYS.AGENDA);
  if (cached && cached.date === today) {
    // Vérification si les données sont récentes (moins de 24h)
    const cacheDate = new Date(cached.date);
    const currentDate = new Date();
    const diffTime = currentDate.getTime() - cacheDate.getTime();
    const diffDays = diffTime / (1000 * 3600 * 24); // Conversion en jours
    if (diffDays < CACHE_EXPIRY_DAYS) {
      return cached.data;
    }
  }
  return null; // Besoin de re-fetch
}


// --------- LIST ---------

export async function saveList(data: { items: any[]; totalPages: number }) {
  await saveAgendaData(KEYS.LIST, { date: today, data });
}

export async function getCachedList() {
  const cached = await getAgendaData(KEYS.LIST);
  if (cached && cached.date === today) {
    if (cached.data && Array.isArray(cached.data.items) && typeof cached.data.totalPages === 'number') {
      return cached.data as { items: any[]; totalPages: number };
    }
  }
  return null;
}

// --------- FORECASTS ---------

export async function saveForecasts(data: { items: any[]; totalPages: number }) {
  await saveAgendaData(KEYS.FORECASTS, { date: today, data });
}

export async function getCachedForecasts() {
  const cached = await getAgendaData(KEYS.FORECASTS);
  if (cached && cached.date === today) {
    if (cached.data && Array.isArray(cached.data.items) && typeof cached.data.totalPages === 'number') {
      return cached.data as { items: any[]; totalPages: number };
    }
  }
  return null;
}
