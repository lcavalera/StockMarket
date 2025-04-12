// i18n.js
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import enTranslations from './Locales/en/translation.json';
import frTranslations from './Locales/fr/translation.json';

const language = sessionStorage.getItem('language') || 'fr'; // Récupérer la langue stockée ou utiliser une langue par défaut

// Configuration d'i18next
i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        translation: enTranslations,
      },
      fr: {
        translation: frTranslations,
      },
    },
    lng: language, // Utiliser la langue stockée
    fallbackLng: 'en', // Langue de secours
    interpolation: {
      escapeValue: false,
    },
  });

export default i18n;
