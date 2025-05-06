import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import '../Css/list.css'
import IndiceExplorer from '../Components/indiceExplorer';
import { useAuth } from "../Auth/autoContext";
import { getForecasts, getAgendaSemaine } from '../Data/dataApi';
import { getCachedForecasts, saveForecasts } from '../Data/dataCache';  // Importation des fonctions
import AgendaMini from '../Components/agendaMini';

const Forecasts: React.FC = () => {
    //  Configuration for using translation with json file
    const { t } = useTranslation();
    const { role } = useAuth();
    const navigate = useNavigate();

    return (
        <>
            <h1 className='agenda-title'>{t('agenda.title')}</h1>
            {role === 'premium' ? (
                <AgendaMini fetchData={getAgendaSemaine} returnUrl="forecasts" />
            ) : (
                <div className="agenda-upgrade">
                    <p>{t('agenda.accessLimited')}</p>
                    <button onClick={() => navigate('/abonnements')}>
                        {t('agenda.upgrade')}
                    </button>
                </div>
            )}

            <h1 className='list-title'>{t('list.title')}</h1>
            <IndiceExplorer
                fetchData={getForecasts}
                returnUrl="forecasts"
                saveCache={(data, searchTerm, exchangeFilter, sortOrder, page) =>
                    saveForecasts(data, searchTerm, exchangeFilter, sortOrder, page)
                }
                getCachedData={(searchTerm, exchangeFilter, sortOrder, page) =>
                    getCachedForecasts(searchTerm, exchangeFilter, sortOrder, page)
                }
            />
        </>
    );
};

export default Forecasts;
