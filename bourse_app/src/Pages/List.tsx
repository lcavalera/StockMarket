import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import '../Css/list.css'
import IndiceExplorer from '../Components/indiceExplorer';
import { useAuth } from "../Auth/autoContext";
import { getIndices, getAgendaSemaine } from '../Data/dataApi';
import { getCachedList, saveList } from '../Data/dataCache';  // Importation des fonctions
import AgendaMini from '../Components/agendaMini';


const List: React.FC = () => {
    //  Configuration for using translation with json file
    const { t } = useTranslation();
    const { role } = useAuth();
    const navigate = useNavigate();

    return (
        <>
            <h1 className='agenda-title'>{t('agenda.title')}</h1>
            {role === 'premium' ? (
                <AgendaMini fetchData={getAgendaSemaine} returnUrl="list" />
            ):
            role === 'public' ? (
                <div className="agenda-upgrade">
                    <p>{t('agenda.accessPremium')}</p>
                    <button className="button-blue" onClick={() => navigate('/payment')}>
                        {t('agenda.upgrade')}
                    </button>
                </div>
            ):(
                <div className="agenda-upgrade">
                    <p>{t('agenda.accessLimited')}</p>
                    <button className="button-blue" onClick={() => navigate('/registerForm')}>
                        {t('agenda.register')}
                    </button>
                </div>
            )}
            <br />
            <h1 className='list-title'>{t('list.title')}</h1>
            <IndiceExplorer
                fetchData={getIndices}
                returnUrl="list"
                saveCache={(data, searchTerm, exchangeFilter, sortOrder, page) =>
                    saveList(data, searchTerm, exchangeFilter, sortOrder, page)
                }
                getCachedData={(searchTerm, exchangeFilter, sortOrder, page) =>
                    getCachedList(searchTerm, exchangeFilter, sortOrder, page)
                }
/>
        </>
    );
};

export default List;
