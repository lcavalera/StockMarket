// Home.tsx
import React from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import LoginForm from "../Components/loginForm";
import '../Css/home.css'

const Home: React.FC = () => {
    //  Configuration for using translation with json file
    const { t } = useTranslation();
    const isLoggedIn = !!sessionStorage.getItem('token');

    return (
        <>
            <div className='column-home'>
                <div className="column-home-left">
                    <div className='title'>
                        <h2>{t('home.welcome')}</h2>
                    </div>
                    <h2>{t('home.description')}</h2><br /><br />
                    <h2 id='subtitle'>{t('home.subtitle')}</h2><br />
                    <Link to="/registerForm"><button className="button-gray" id="button-main">{t('home.buttonInscription')}</button></Link>
                </div>
                {!isLoggedIn && 
                <div className="column-home-right">
                    <LoginForm />
                </div>}
            </div>
        </>
    )
};

export default Home;
