import React, { useEffect, useState } from 'react';
import { IndiceDTO } from '../types'; // adapte si besoin
import { getIndices } from '../Data/dataApi'; // adapte si besoin
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { getCachedAgenda, saveAgenda } from '../Data/dataCache.ts'; // Importer les fonctions d'IndexedDB
import '../Css/agenda.css'

interface AgendaMiniProps {
    fetchData: typeof getIndices;
    returnUrl: string;
}
  

export default function AgendaMini({ fetchData , returnUrl }: AgendaMiniProps) {
    const [agendaData, setAgendaData] = useState<Record<string, IndiceDTO[]>>({});
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [popupData, setPopupData] = useState<{ date: string; items: IndiceDTO[] } | null>(null);
    const { t, i18n } = useTranslation(); // 💬 récupère la langue active en haut de ton composant

    

    useEffect(() => {
        const loadData = async () => {
            try {
                console.log('Fetching data...');
                setIsLoading(true);

                let response = await getCachedAgenda(); // 👈 ici tu utilises le cache
                if (!response) {
                    console.log('No cached agenda, fetching from API...');
                    const apiResponse = await fetchData('', '', '', 1, 1000);
                    await saveAgenda(apiResponse.items); // 👈 sauve dans IndexedDB
                    response = apiResponse.items;
                  }
                const today = new Date();

                // Récupérer hier (le jour avant aujourd'hui)
                const yesterday = new Date(today);
                yesterday.setDate(today.getDate() - 1);  // Ajuste la date pour le jour précédent
                yesterday.setHours(0, 0, 0, 0); // Fixer à minuit

                // Créer un tableau de dates avec hier et les 6 jours suivants
                const next7Days = [yesterday,...[...Array(6)].map((_, i) => {
                    const date = new Date();
                    date.setDate(today.getDate() + i);
                    date.setHours(0, 0, 0, 0); // clean l'heure
                    return date;
                })];

                const agenda: Record<string, IndiceDTO[]> = {};

                next7Days.forEach(date => {
                    const key = date.toISOString().split('T')[0]; // format AAAA-MM-JJ
                    agenda[key] = [];
                });

                response.forEach((indice: IndiceDTO) => {
                    indice.datesExercicesFinancieres.forEach(dateExercice => {
                        const dateKey = new Date(dateExercice).toISOString().split('T')[0];
                        if (agenda[dateKey]) {
                            agenda[dateKey].push(indice);
                        }
                    });
                });

                // Trie les listes par volume
                Object.keys(agenda).forEach(key => {
                    agenda[key] = agenda[key].sort((a, b) => b.regularMarketVolume - a.regularMarketVolume);
                });

                setAgendaData(agenda);

            } catch (error) {
                console.error('Erreur de chargement de l\'agenda', error);
            } finally{
                setIsLoading(false);
            }
        };

        loadData();
    }, [fetchData]);

    if (isLoading) {
        return <div  className='chargement-agenda'>
                <div className="spinner"></div><br /> {/* Ajoute ici ton spinner */}
                <div className='chargement-agenda-titre'>{t('agenda.loading')}</div>
            </div>
    }



    const formatDate = (dateString: string) => {
        // Crée une instance de la date en UTC, sans affecter la date
        const date = new Date(dateString + "T00:00:00"); // Ajoute l'heure et force UTC
        // Formater la date sans modification
        return date.toLocaleDateString(i18n.language, { day: 'numeric', month: 'long' });
    };

    return (
        <>
        <div className="agenda-mini-container">
            {Object.entries(agendaData).map(([dateKey, indices]) => {
                const today = new Date();
                today.setHours(0, 0, 0, 0); // reset heure pour comparer que la date
                const isToday = dateKey === today.toISOString().split('T')[0];
                return (
                    <div key={dateKey} className="agenda-day-card">
                        <h3 className={isToday ? "today" : ""}>{formatDate(dateKey)}</h3>
                        <ul>
                            {indices.slice(0, 5).map(indice => (
                                <li key={indice.id}>
                                    <Link to={`/details/${indice.symbol}?returnUrl=${returnUrl}`}>
                                        <span className="symbol">{indice.symbol}</span> - {indice.name}
                                    </Link> 
                                </li>
                            ))}
                        </ul>
                        {indices.length > 5 && (
                            <button
                                    className="show-more-btn"
                                    onClick={() => setPopupData({ date: formatDate(dateKey), items: indices })}
                                >
                                    {t('agenda.button')}
                                </button>
                        )}
                    </div>
                );
            })}
        </div>
            {popupData && (
                <div className="popup-overlay">
                    <div className="popup-content">
                        <h2>{popupData.date}</h2>
                        <ul>
                            {popupData.items.map((indice) => (
                                <li key={indice.id}>
                                    <Link to={`/details/${indice.symbol}?returnUrl=${returnUrl}`}>
                                        <span className="symbol">{indice.symbol}</span> - {indice.name}
                                    </Link>               
                                </li>
                            ))}
                        </ul>
                        <button className="close-btn" onClick={() => setPopupData(null)}>{t('agenda.close')}</button>
                    </div>
                </div>
            )}
        </>
    );
}
