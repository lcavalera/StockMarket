import React from 'react';
import { useTranslation } from 'react-i18next';
import '../Css/list.css'
import IndiceExplorer from '../Components/indiceExplorer.tsx';
import { getForecasts } from '../Data/dataApi.ts';
import { getCachedForecasts, saveForecasts } from '../Data/dataCache.ts';  // Importation des fonctions

import AgendaMini from '../Components/agendaMini.tsx';


const Forecasts: React.FC = () => {
    //  Configuration for using translation with json file
    const { t } = useTranslation();

    return (
        <>
            <h1 className='agenda-title'>{t('agenda.title')}</h1>
            <AgendaMini fetchData={getForecasts} returnUrl="forecast" />

            <h1 className='list-title'>{t('list.title')}</h1>
            <IndiceExplorer
                fetchData={getForecasts}
                returnUrl="forecasts"
                saveCache={saveForecasts}
                getCachedData={getCachedForecasts}
            />
        </>
    );
};

export default Forecasts;
