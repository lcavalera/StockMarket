import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from "../Auth/autoContext";
import LanguageDropdown from '../Components/languageDropDown'; // Make sure the path is correct
import type { UserProfile } from '../Data/typeusers';
import { FaUser } from 'react-icons/fa';

interface HeaderProps {
    // You can add props here if needed
    // refreshCount: number;
}

// const Header: React.FC<HeaderProps> = ({ refreshCount }) => {
const Header: React.FC<HeaderProps> = () => {
    const [isLogin, setIsLogin] = useState<boolean | false>(false);
    const [user, setUser] = useState<UserProfile | null>(null);
    const { role, logout } = useAuth();

    const handleLogin = () => {
        const token = sessionStorage.getItem('token');
        const user = sessionStorage.getItem('user');
        if (token && user) {
            const userParsed: UserProfile = JSON.parse(user);
            setUser(userParsed);
            setIsLogin(true);
        } else {
            setUser(null);
            setIsLogin(false);
        }
    };

    const handleLogout = () => {
        sessionStorage.removeItem('token');
        sessionStorage.removeItem('user'); // si tu stockes le user aussi
        logout(); // ← vide aussi le rôle
        setUser(null);
        setIsLogin(false);
        window.location.href = "/"; // Redirige vers l'accueil
    };

    useEffect(() => {
        handleLogin();
    }, []);

    // Expose handleLogin if needed
    useEffect(() => {
        const handleRefresh = () => {
            handleLogin();
        };

        window.addEventListener('refresh-login', handleRefresh);
        return () => {
            window.removeEventListener('refresh-login', handleRefresh);
        };
    }, []);
    
    // useEffect(() => {
    //     const handleRefresh = () => {
    //         const token = localStorage.getItem('token');
    //         const userLogin = getUserFromSessionStorage();
    //         if (token && userLogin) {
    //             setUser(userLogin);
    //             setIsLogin(true);
    //         } else {
    //             setUser(null);
    //             setIsLogin(false);
    //         }
    //     };
    
    //     window.addEventListener('refresh-login', handleRefresh);
        
    //     // Initial check
    //     handleRefresh();
    
    //     return () => {
    //         window.removeEventListener('refresh-login', handleRefresh);
    //     };
    // }, []);

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
                {role === 'premium' ? (
                    <Link to="/forecasts"><h1>{t('header.forecasts')}</h1></Link>
                ) : (
                    <Link to="/registerForm"><h1>{t('header.forecasts')}</h1></Link>
                )}
            </div>
            <div id="menu-header">           
                {!isLogin ? (
                    <div className='contLogin'>
                        <Link to="/login"><button className="button-blue">{t('header.buttons.login')}</button></Link>
                        <Link to="/registerForm"><button className="button-gray">{t('header.buttons.register')}</button></Link>
                    </div>) : (
                <div className='contLogin'>
                <div className="dropdown">
                    <button className="dropbtn">                
                        Bienvenue {user?.firstName}
                        <FaUser size={24} />
                    </button>
                    <div className="dropdown-content">
                        <Link to="/profile">{t('header.menu.profile')}</Link>
                        <button id='logout' onClick={handleLogout}>{t('header.menu.logout')}</button>
                    </div>
                </div>
            </div>
                )}
                <div className='LanguageDropdown'><LanguageDropdown /></div>
            </div>
        </header>
    );
}

export default Header;