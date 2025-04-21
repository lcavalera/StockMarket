// src/Data/indexedDB.ts

import { openDB } from 'idb';

const DB_NAME = 'bourseAgendaDB';
const STORE_NAME = 'agendaStore';
const DB_VERSION = 1;

// Fonction pour ouvrir la base
export async function openAgendaDB() {
  return openDB(DB_NAME, DB_VERSION, {
    upgrade(db) {
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME);
      }
    }
  });
}

// Fonction pour enregistrer un élément
export async function saveAgendaData(key: string, value: any) {
  const db = await openAgendaDB();
  const tx = db.transaction(STORE_NAME, 'readwrite');
  await tx.store.put(value, key);
  await tx.done;
}

// Fonction pour récupérer un élément
export async function getAgendaData(key: string) {
  const db = await openAgendaDB();
  const value = await db.get(STORE_NAME, key);
  return value;
}
