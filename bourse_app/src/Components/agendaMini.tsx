import { useEffect, useState, useCallback } from 'react';
import { IndiceDTO } from '../types';
import { getAgendaSemaine } from '../Data/dataApi';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { getCachedAgenda, saveAgenda } from '../Data/dataCache';
import '../Css/agenda.css';

interface AgendaMiniProps {
    fetchData: typeof getAgendaSemaine;
    returnUrl: string;
}

// ✅ Fonction utilitaire pour convertir une date SQL en Date valide JS
const parseDate = (dateStr: string): Date => {
    return new Date(dateStr.replace(' ', 'T'));
};

const AgendaMini = ({ fetchData, returnUrl }: AgendaMiniProps) => {
    const [agendaData, setAgendaData] = useState<Record<string, IndiceDTO[]>>({});
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [popupData, setPopupData] = useState<{ date: string; items: IndiceDTO[] } | null>(null);
    const { t, i18n } = useTranslation();

    const loadData = useCallback(async () => {
        try {
            setIsLoading(true);

            let response = await getCachedAgenda();

            if (!response) {
                console.log('No cached agenda, fetching from API...');
                response = await fetchData();
                await saveAgenda(response);
            }

            const today = new Date();
            const yesterday = new Date(today);
            yesterday.setDate(today.getDate() - 1);
            yesterday.setHours(0, 0, 0, 0);

            const next7Days = [yesterday, ...Array.from({ length: 6 }, (_, i) => {
                const date = new Date();
                date.setDate(today.getDate() + i);
                date.setHours(0, 0, 0, 0);
                return date;
            })];
            const agenda: Record<string, IndiceDTO[]> = {};
            next7Days.forEach(date => {
                const key = date.toISOString().split('T')[0];
                agenda[key] = [];
            });

            // ✅ Ajouter l'indice uniquement à la date la plus proche dans la semaine
            response.forEach((indice: IndiceDTO) => {
                if (!indice.datesExercicesFinancieres || indice.datesExercicesFinancieres.length === 0) return;

                const datesValides = indice.datesExercicesFinancieres
                    .map(dateStr => typeof dateStr === 'string' ? parseDate(dateStr) : dateStr)
                    .filter(date => {
                        const key = date.toISOString().split('T')[0];
                        return agenda.hasOwnProperty(key);
                    });

                if (datesValides.length === 0) return;

                const earliest = new Date(Math.min(...datesValides.map(d => d.getTime())));
                const dateKey = earliest.toISOString().split('T')[0];

                agenda[dateKey].push(indice);
            });


            // Trier par volume décroissant
            Object.keys(agenda).forEach(key => {
                agenda[key] = agenda[key].sort((a, b) => b.regularMarketVolume - a.regularMarketVolume);
            });

            setAgendaData(agenda);

        } catch (error) {
            console.error("Erreur de chargement de l'agenda", error);
        } finally {
            setIsLoading(false);
        }
    }, [fetchData]);

    useEffect(() => {
        loadData();
    }, [loadData]);

    if (isLoading) {
        return (
            <div className='chargement-agenda'>
                <div className="spinner"></div><br />
                <div className='chargement-agenda-titre'>{t('agenda.loading')}</div>
            </div>
        );
    }

    const formatDate = (dateString: string) => {
        const date = new Date(dateString + "T00:00:00");
        return date.toLocaleDateString(i18n.language, { day: 'numeric', month: 'long' });
    };

    const handleScroll = (e: React.UIEvent<HTMLElement>) => {
        const target = e.target as HTMLElement;
        const bottom = target.scrollHeight === target.scrollTop + target.clientHeight;
        if (bottom) {
            loadData(); // Recharge si on atteint le bas (optionnel)
        }
    };

    return (
        <>
            <div className="agenda-mini-container" onScroll={handleScroll}>
                {Object.entries(agendaData).map(([dateKey, indices]) => {
                    const today = new Date();
                    today.setHours(0, 0, 0, 0);
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
                            {popupData.items.map(indice => (
                                <li key={indice.id}>
                                    <Link to={`/details/${indice.symbol}?returnUrl=${returnUrl}`}>
                                        <span className="symbol">{indice.symbol}</span> - {indice.name}
                                    </Link>
                                </li>
                            ))}
                        </ul>
                        <button className="close-btn" onClick={() => setPopupData(null)}>
                            {t('agenda.close')}
                        </button>
                    </div>
                </div>
            )}
        </>
    );
};

export default AgendaMini;
