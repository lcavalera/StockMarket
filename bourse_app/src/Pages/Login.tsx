// Login.tsx
import React from 'react';
import { useTranslation } from 'react-i18next';
import LoginForm from "../Components/loginForm";
import '../Css/home.css'

const Login: React.FC = () => {
    //  Configuration for using translation with json file
    const { t } = useTranslation();

    return (
        <>
            <div className="column-home-right">
                <LoginForm />
            </div>
        </>
    )
};

export default Login;
