import React from 'react';
import { useTranslation } from 'react-i18next';
import '../Css/list.css'
import IndiceExplorer from '../Components/indiceExplorer.tsx';
import { getIndices } from '../Data/dataApi.ts';
import { getCachedList, saveList } from '../Data/dataCache.ts';  // Importation des fonctions
import AgendaMini from '../Components/agendaMini.tsx';


const List: React.FC = () => {
    //  Configuration for using translation with json file
    const { t } = useTranslation();
    
    return (
        <>
            <h1 className='agenda-title'>{t('agenda.title')}</h1>
            <AgendaMini fetchData={getIndices} returnUrl="list" />

            <h1 className='list-title'>{t('list.title')}</h1>
            <IndiceExplorer
                fetchData={getIndices}
                returnUrl="list"
                saveCache={saveList}
                getCachedData={getCachedList}
            />
        </>
    );
};

export default List;
