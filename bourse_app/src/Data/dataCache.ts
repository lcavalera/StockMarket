import { saveData, getData, connexionDB } from '../Data/indexedDB';

const today = new Date().toISOString().split('T')[0];
const CACHE_EXPIRY_DAYS = 1; // Par exemple 1 jour

// Clés utilisées dans IndexedDB
const KEYS = {
  AGENDA: 'agendaData',
  LIST: 'listData',
  FORECASTS: 'forecastsData'
};

type KeyType = keyof typeof KEYS; // Type qui permet seulement 'AGENDA', 'LIST', 'FORECASTS'
// ---------- UTILS ----------

export async function clearAllCache() {
  const db = await connexionDB();
  const tx = db.transaction('store', 'readwrite');
  const store = tx.objectStore('store');

  const allKeys = await store.getAllKeys();
  for (const key of allKeys) {
    await store.delete(key);
  }

  await tx.done;
  console.log('✅ Cache cleared from IndexedDB.');
}

function getListCacheKey(key: KeyType, search: string, exchange: string, sort: string, page: number): string {
  const normalized = (str: string) => str.trim().toLowerCase() || 'all';
  return `${KEYS[key]}_${normalized(search)}_${normalized(exchange)}_${normalized(sort)}_page${page}`;
}

function isCacheValid(date: string): boolean {
  const cacheDate = new Date(date);
  const now = new Date();
  const diffTime = now.getTime() - cacheDate.getTime();
  const diffDays = diffTime / (1000 * 3600 * 24);
  return diffDays < CACHE_EXPIRY_DAYS;
}

async function saveCache<T>(key: string, data: T) {
  await saveData(key, { date: today, data });
}

async function getCache<T>(key: string): Promise<T | null> {
  const cached = await getData(key);
  if (cached && isCacheValid(cached.date)) {
    return cached.data as T;
  }
  return null;
}

// --------- AGENDA ---------

export async function saveAgenda(items: any[]) {
  await saveCache(KEYS.AGENDA, items);
}

export async function getCachedAgenda(): Promise<any[] | null> {
  return await getCache<any[]>(KEYS.AGENDA);
}

// --------- LIST ---------

export async function saveList(
  data: { items: any[]; totalPages: number },
  search: string,
  exchange: string,
  sort: string,
  page: number
) {
  const key = getListCacheKey('LIST', search, exchange, sort, page);
  await saveCache(key, data);
}

export async function getCachedList(
  search: string,
  exchange: string,
  sort: string,
  page: number
): Promise<{ items: any[]; totalPages: number } | null> {
  const key = getListCacheKey('LIST', search, exchange, sort, page);
  const data = await getCache<{ items: any[]; totalPages: number }>(key);
  return data?.items && typeof data.totalPages === 'number' ? data : null;
}


// --------- FORECASTS ---------

export async function saveForecasts(
  data: { items: any[]; totalPages: number },
  search: string,
  exchange: string,
  sort: string,
  page: number
) {
  const key = getListCacheKey('FORECASTS', search, exchange, sort, page);
  await saveCache(key, data);
}

export async function getCachedForecasts(
  search: string,
  exchange: string,
  sort: string,
  page: number
): Promise<{ items: any[]; totalPages: number } | null> {
  const key = getListCacheKey('FORECASTS', search, exchange, sort, page);
  const data = await getCache<{ items: any[]; totalPages: number }>(key);
  return data?.items && typeof data.totalPages === 'number' ? data : null;
}
