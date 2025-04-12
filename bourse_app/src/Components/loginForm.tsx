import React, { useState, ChangeEvent, FormEvent } from 'react';
import { loginApi } from '../Data/profileApi.ts';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

// Define the shape of the form data
interface FormData {
    username: string;
    password: string;
}

// Define the shape of the validation errors
interface FormErrors {
    username?: string;
    password?: string;
}

const LoginForm: React.FC = () => {
    const [formData, setFormData] = useState<FormData>({
        username: '',
        password: ''
    });

    // const { onRefresh } = useRefresh();

    const navigate = useNavigate();
    // const headerDomNode = document.getElementById('header');
    // const root = document.getElementById('root');
    // const rootElement  = document.getElementById('root');

    const [errors, setErrors] = useState<FormErrors>({});

    // Handle input change events
    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData({
            ...formData,
            [name]: value
        });
    };

    // Validate form inputs
    const validateForm = (): boolean => {
        let valid = true;
        const newErrors: FormErrors = {};

        if (!formData.username) {
            newErrors.username = 'Le courriel est obligatoire';
            valid = false;
        }

        if (!formData.password) {
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
            
            // Handle form submission logic here
            const apiCall = loginApi(formData);

            apiCall
                .then((data => {

                    switch(data?.user.role){
                        case 'Admin':{
                            navigate('/admin_index')
                            // onRefresh();
                            return
                        }
                        case 'Public':{
                            const state = data.user.inscriptions.find(i => i.state === 'Stanby');
                            if(state){
                                navigate('/messwaiting')
                                
                            }
                            else{
                                navigate('/account_index')
                            }
                            // if (rootElement) {
                            //     const headerRoot = ReactDOM.createRoot(rootElement );
                            //     // console.log(headerRoot);
                            //     headerRoot.render(<Layout />);
                            // }
                            // onRefresh();
                            return
                        }
                        case 'Manager':{
                            navigate('/account_index')
                            // onRefresh();
                            return
                        }
                        case 'Other':{
                            navigate('/account_index')
                            // onRefresh();
                            return
                        }
                    }

                }))
                .catch(error => {
                    console.error(error.message);
                    alert(error.message);
                });


            // if (formData.username == 'admin@classe-museewahta.org') {
            //     navigate('/admin_index');
            // } else {
            //     navigate('/account_index');
            // };

            // login();
            console.log('Form submitted:', formData);
            // Example: Send data to an API or update state
        }
    };

    const { t } = useTranslation();

    return (
        <div className="container-form">
            <h2>{t('loginform.title')}</h2>
            <form className="login-form" onSubmit={handleSubmit}>
                <div className="form-group">
                    <label htmlFor="username">{t('loginform.label.email')}</label><br />
                    <input
                        placeholder={t('loginform.input.email')}
                        type="text"
                        id="username"
                        name="username"
                        value={formData.username}
                        onChange={handleChange} // Call the function to assign the entered value
                    />
                    {errors.username && <p className="error">{errors.username}</p>}
                </div><br />
                <div className="form-group">
                    <label htmlFor="password">{t('loginform.label.password')}</label><br />
                    <input
                        placeholder={t('loginform.input.password')}
                        type="password"
                        id="password"
                        name="password"
                        value={formData.password}
                        onChange={handleChange} // Call the function to assign the entered value
                    />
                    {errors.password && <p className="error">{errors.password}</p>}
                </div><br />
                <button className='button-red' id='button-login' type="submit">{t('loginform.button')}</button><br /><br />
                <a href="/">{t('loginform.link')}</a>
            </form>
        </div>
    );
};

export default LoginForm;