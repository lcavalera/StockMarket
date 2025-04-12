import React from 'react';
import { useTranslation } from 'react-i18next';
import '../Css/list.css'
import IndiceExplorer from '../Components/indiceExplorer.tsx';


const List: React.FC = () => {
    //  Configuration for using translation with json file
    const { t } = useTranslation();

    return (
        <>
            <h1 className='list-title'>{t('list.title')}</h1>
            <IndiceExplorer />
        </>
    );
};

export default List;
