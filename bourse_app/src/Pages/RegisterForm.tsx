import React, { useState, ChangeEvent, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { registerUser } from '../Data/dataApi'; // Importer la fonction d'enregistrement
// import { useAuth } from "../Auth/autoContext";


const RegisterForm: React.FC = () => {
    const [formData, setFormData] = useState({
        userName: '',
        password: '',
        firstName: '',
        lastName: '',
        phone: '',
        role: '', // Par défaut, l'utilisateur est "Public"
        address: '',
        postalCode: '',
        city: ''
    });

    // const [subscriptionType, setSubscriptionType] = useState<'free' | 'premium'>('free');
    const [errors, setErrors] = useState<any>({});
    // const { login } = useAuth();
    const navigate = useNavigate();
    const { t } = useTranslation();

    // Fonction pour gérer les changements dans le formulaire
    const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = e.target;
        setFormData({...formData,[name]: value});
    };

    // const handleSubscriptionChange = (e: ChangeEvent<HTMLInputElement>) => {
    //     setSubscriptionType(e.target.value as 'free' | 'premium');
    // };

    // Validation du formulaire
    const validateForm = (): boolean => {
        let valid = true;
        const newErrors: any = {};

        if (!formData.userName) {
            newErrors.userName = t('registerform.error-email2');
            valid = false;
        }else{
            // Regex pour vérifier Majuscule + Minuscule + Chiffre + Caractère spécial
            const passwordRegex = /^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).+$/;
            if (!passwordRegex.test(formData.password)) {
                newErrors.password = t('registerform.error-password1');
                valid = false;
            }
        }
        if (!formData.password) {
            newErrors.password = t('registerform.error-password2');
            valid = false;
        }
        if (!formData.firstName) {
            newErrors.firstName = t('registerform.error-firstName');
            valid = false;
        }
        if (!formData.lastName) {
            newErrors.lastName = t('registerform.error-lastName');
            valid = false;
        }
        if (!formData.phone) {
            newErrors.phone = t('registerform.error-phone');
            valid = false;
        }
        if (!formData.address) {
            newErrors.address = t('registerform.error-address');
            valid = false;
        }
        if (!formData.postalCode) {
            newErrors.postalCode = t('registerform.error-zipCode');
            valid = false;
        }
        if (!formData.city) {
            newErrors.city = t('registerform.error-city');
            valid = false;
        }
        if (!formData.role){
            newErrors.role = t('registerform.error-role');
            valid = false;
        }

        setErrors(newErrors);
        return valid;
    };

    // Fonction pour gérer la soumission du formulaire
    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (!validateForm()) return;

        if (formData.role === 'premium') {
            // Stocker temporairement les infos dans localStorage ou context si besoin
            sessionStorage.setItem('pendingRegistration', JSON.stringify(formData));
            navigate('/payment');
        } else {
            const result = await registerUser(formData);
            if (result.success) {
                navigate('/login');
            } else {
                setErrors({ ...errors, submit: result.message });
            }
        }

    };

    return (
        <div className="container-form">
            <h2 id='form-title'>{t('registerform.title')}</h2>
            <form className="register-form" onSubmit={handleSubmit}>
                <div className="form-group">
                    <label>{t('registerform.email')}</label>
                    <input type="email" name="userName" placeholder={t('registerform.input-email')} value={formData.userName} onChange={handleChange} />
                    {errors.userName && <p className="error">{errors.userName}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.password')}</label>
                    <input type="password" name="password" placeholder={t('registerform.input-password')} value={formData.password} onChange={handleChange} />
                    {errors.password && <p className="error">{errors.password}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.firstname')}</label>
                    <input type="text" name="firstName" placeholder={t('registerform.input-firstname')} value={formData.firstName} onChange={handleChange} />
                    {errors.firstName && <p className="error">{errors.firstName}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.lastname')}</label>
                    <input type="text" name="lastName" placeholder={t('registerform.input-lastname')} value={formData.lastName} onChange={handleChange} />
                    {errors.lastName && <p className="error">{errors.lastName}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.phone')}</label>
                    <input type="text" name="phone" placeholder={t('registerform.input-phone')} value={formData.phone} onChange={handleChange} />
                    {errors.phone && <p className="error">{errors.phone}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.address')}</label>
                    <input type="text" name="address" placeholder={t('registerform.input-address')} value={formData.address} onChange={handleChange} />
                    {errors.address && <p className="error">{errors.address}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.zipCode')}</label>
                    <input type="text" name="postalCode" placeholder={t('registerform.input-zipCode')} value={formData.postalCode} onChange={handleChange} />
                    {errors.postalCode && <p className="error">{errors.postalCode}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.city')}</label>
                    <input type="text" name="city" placeholder={t('registerform.input-city')} value={formData.city} onChange={handleChange} />
                    {errors.city && <p className="error">{errors.city}</p>}
                </div><br />
                <div className="form-group">
                    <label id='subscription'>{t('registerform.subscription')}</label>
                    <div className="subscription-options">
                        <label className={`option-card ${formData.role === 'public' ? 'selected' : ''}`}>
                            <input
                                type="radio"
                                name="role"
                                value="public"
                                checked={formData.role === 'public'}
                                onChange={handleChange}
                            />
                            <div>
                                <strong>{t('registerform.free')}</strong>
                                <p>{t('registerform.freeDesc')}</p><br />
                                <p>Ajouter description...</p>
                            </div>
                        </label>

                        <label className={`option-card ${formData.role === 'premium' ? 'selected' : ''}`}>
                            <input
                                type="radio"
                                name="role"
                                value="premium"
                                checked={formData.role === 'premium'}
                                onChange={handleChange}
                            />
                            <div>
                                <strong>{t('registerform.premium')}</strong>
                                <p>{t('registerform.premiumDesc')}</p><br />
                                <p>Ajouter description...</p>
                            </div>
                        </label>           
                    </div>
                    {errors.role && <p className="error">{errors.role}</p>}
                </div>
                <br />

                {errors.submit && <p className="error">{errors.submit}</p>}

                <button className='button-blue' id='button-register' type="submit">{t('registerform.button')}</button>
            </form>
        </div>
    );
};

export default RegisterForm;
