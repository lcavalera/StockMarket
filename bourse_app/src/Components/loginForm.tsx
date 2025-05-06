import React, { useState, ChangeEvent, FormEvent } from 'react';
import { login } from '../Data/dataApi';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { refreshHeaderLogin } from '../Data/dataApi';
import { useAuth } from '../Auth/autoContext'; // <-- Import du contexte Auth
import type { UserRole } from '../Auth/autoContext';

// Define the shape of the form data
interface LoginModel {
    username: string;
    password: string;
}

// Define the shape of the validation errors
interface FormErrors {
    username?: string;
    password?: string;
}

const LoginForm: React.FC = () => {
    const [loginModel, setFormData] = useState<LoginModel>({
        username: '',
        password: ''
    });

    const [errors, setErrors] = useState<FormErrors>({});
    const navigate = useNavigate();
    const { t } = useTranslation();
    const { login: setAuthRole } = useAuth(); // <-- Récupère la fonction pour définir le rôle

    // Handle input change events
    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData({
            ...loginModel,
            [name]: value
        });
    };

    // Validate form inputs
    const validateForm = (): boolean => {
        let valid = true;
        const newErrors: FormErrors = {};

        if (!loginModel.username) {
            newErrors.username = 'Le courriel est obligatoire';
            valid = false;
        }

        if (!loginModel.password) {
            newErrors.password = 'Le mot de passe est obligatoire';
            valid = false;
        }

        setErrors(newErrors);
        return valid;
    };

    // Handle form submission
    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (validateForm()) {
            try {
                const data = await login({
                    userName: loginModel.username,
                    password: loginModel.password
                });

                refreshHeaderLogin();
                
                const roleFromApi = data?.user?.role?.toLowerCase(); // 'Admin' -> 'admin'
                const validRoles: UserRole[] = ['anonymous', 'public', 'premium'];
                
                if (validRoles.includes(roleFromApi as UserRole)) {
                    setAuthRole(roleFromApi as UserRole);
                } else {
                    setAuthRole('anonymous');
                }
                

                navigate('/list');

            } catch (error: any) {
                console.error(error.message);
                alert(error.message);
            }
        }       
    };

    return (
        <div className="container-form">
            <h2 id='form-title'>{t('loginform.title')}</h2>
            <form className="login-form" onSubmit={handleSubmit}>
                <div className="form-group">
                    <label htmlFor="username">{t('loginform.label.email')}</label>
                    <input
                        placeholder={t('loginform.input.email')}
                        type="text"
                        id="username"
                        name="username"
                        value={loginModel.username}
                        onChange={handleChange} // Call the function to assign the entered value
                    />
                    {errors.username && <p className="error">{errors.username}</p>}
                </div><br />
                <div className="form-group">
                    <label htmlFor="password">{t('loginform.label.password')}</label>
                    <input
                        placeholder={t('loginform.input.password')}
                        type="password"
                        id="password"
                        name="password"
                        value={loginModel.password}
                        onChange={handleChange} // Call the function to assign the entered value
                    />
                    {errors.password && <p className="error">{errors.password}</p>}
                </div><br />
                <button className='button-blue' id='button-login' type="submit">{t('loginform.button')}</button><br /><br />
                <a href="/">{t('loginform.link')}</a>
            </form>
        </div>
    );
};

export default LoginForm;