// src/Data/indexedDB.ts

import { openDB } from 'idb';

const DB_NAME = 'bourseDB';
const STORE_NAME = 'store';
const DB_VERSION = 1;


// Fonction pour ouvrir la base
export async function connexionDB() {
  return openDB(DB_NAME, DB_VERSION, {
    upgrade(db) {
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME);
      }
    }
  });
}

// Fonction pour enregistrer un élément
export async function saveData(key: string, value: any) {
  const db = await connexionDB();
  const tx = db.transaction(STORE_NAME, 'readwrite');
  await tx.store.put(value, key);
  await tx.done;
}

// Fonction pour récupérer un élément
export async function getData(key: string) {
  const db = await connexionDB();
  const value = await db.get(STORE_NAME, key);
  return value;
}
