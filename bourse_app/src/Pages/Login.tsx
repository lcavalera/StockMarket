// Login.tsx
import React from 'react';
import LoginForm from "../Components/loginForm";
import '../Css/home.css'

const Login: React.FC = () => {

    return (
        <>
            <div className="column-home-right">
                <LoginForm />
            </div>
        </>
    )
};

export default Login;
