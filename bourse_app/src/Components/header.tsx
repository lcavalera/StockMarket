import React, { useState, useEffect } from 'react';
import { getUserFromSessionStorage } from '../Data/profileApi.ts';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import LanguageDropdown from '../Components/languageDropDown.tsx'; // Make sure the path is correct
import type { UserProfile } from '../Data/typeUsers';

interface HeaderProps {
    // You can add props here if needed
    // refreshCount: number;
}

// const Header: React.FC<HeaderProps> = ({ refreshCount }) => {
const Header: React.FC<HeaderProps> = () => {
    const [isLogin, setIsLogin] = useState<boolean | false>(false);
    const [user, setUser] = useState<UserProfile | null>(null);

    const handleLogin = () => {
        const token = localStorage.getItem('token'); // Je te conseille localStorage
        const userLogin = getUserFromSessionStorage();
        if (token && userLogin) {
            setUser(userLogin);
            setIsLogin(true);
        } else {
            setUser(null);
            setIsLogin(false);
        }
    };

    useEffect(() => {
        handleLogin();
    }, []);

    // Expose handleLogin if needed
    useEffect(() => {
        const handleRefresh = () => {
            const token = localStorage.getItem('token');
            const userLogin = getUserFromSessionStorage();
            if (token && userLogin) {
                setUser(userLogin);
                setIsLogin(true);
            } else {
                setUser(null);
                setIsLogin(false);
            }
        };
    
        window.addEventListener('refresh-login', handleRefresh);
        
        // Initial check
        handleRefresh();
    
        return () => {
            window.removeEventListener('refresh-login', handleRefresh);
        };
    }, []);

    // useEffect(() => {
    //     const token = sessionStorage.getItem('token');
    //     const userLogin = getUserFromSessionStorage()
    //     // const user = sessionStorage.getItem('username');
    //     if (token && userLogin) {
    //         setUser(userLogin);
    //         setIsLogin(true)
    //     }
    //     // console.log(refreshCount)
    // }, [])

    //  Configuration for using translation with json file
    const { t } = useTranslation();


    
    return (
        <header id='header' className="header">
            <Link to="/"><h1 id="titre-header">StockRank</h1></Link>
            <div id='menu-nav'>
                <Link to="/list"><h1>{t('header.list')}</h1></Link>
                <Link to="/forecasts"><h1>{t('header.forecasts')}</h1></Link>
            </div>
            <div id="menu-header">           
                {!isLogin ? (
                    <div className='contLogin'>
                        <Link to="/login"><button className="button-red">{t('header.buttons.login')}</button></Link>
                        <Link to="/register"><button className="button-gray">{t('header.buttons.register')}</button></Link>
                    </div>) : (
                    <div className='contLogin'>
                        {/* <select>
                            <option value=""><img className='imgProfil' src="profil.jpeg" alt="Profile Utilisateur" /></option>
                            <option ></option>
                        </select> */}
                        <Link to="/profile"><h2 className='welcome'>Bienvenue {user?.firstName} ! </h2><img className='imgProfil' src="profil.jpeg" alt="Profile Utilisateur" /></Link>
                    </div>
                )}
                <div className='LanguageDropdown'><LanguageDropdown /></div>
            </div>
        </header>
    );
}

export default Header;