import React, { useState, ChangeEvent, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { registerUser } from '../Data/dataApi.ts'; // Importer la fonction d'enregistrement


const RegisterForm: React.FC = () => {
    const [formData, setFormData] = useState({
        id: 0,
        email: '',
        passwordHash: '',
        firstName: '',
        lastName: '',
        phone: '',
        role: 'Public', // Par défaut, l'utilisateur est "Public"
        address: '',
        postalCode: '',
        city: ''
    });

    const [errors, setErrors] = useState<any>({});
    const navigate = useNavigate();
    const { t } = useTranslation();

    // Fonction pour gérer les changements dans le formulaire
    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData({...formData,[name]: value});
    };

    // Validation du formulaire
    const validateForm = (): boolean => {
        let valid = true;
        const newErrors: any = {};

        if (!formData.email) {
            newErrors.email = t('registerform.error.email');
            valid = false;
        }
        if (!formData.passwordHash) {
            newErrors.passwordHash = t('registerform.error.password');
            valid = false;
        }
        if (!formData.firstName) {
            newErrors.firstName = t('registerform.error.firstName');
            valid = false;
        }
        if (!formData.lastName) {
            newErrors.lastName = t('registerform.error.lastName');
            valid = false;
        }
        if (!formData.phone) {
            newErrors.phone = t('registerform.error.phone');
            valid = false;
        }
        if (!formData.address) {
            newErrors.address = t('registerform.error.address');
            valid = false;
        }
        if (!formData.postalCode) {
            newErrors.postalCode = t('registerform.error.postalCode');
            valid = false;
        }
        if (!formData.city) {
            newErrors.city = t('registerform.error.city');
            valid = false;
        }

        setErrors(newErrors);
        return valid;
    };

    // Fonction pour gérer la soumission du formulaire
    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (validateForm()) {
            const result = await registerUser(formData); // Appel à la fonction d'enregistrement

            if (result.success) {
                navigate('/login');
            } else {
                setErrors({ ...errors, submit: result.message });
            }
        }
    };

    return (
        <div className="container-form">
            <h2>{t('registerform.title')}</h2>
            <form onSubmit={handleSubmit}>
                <div className="form-group">
                    <label>{t('registerform.label.email')}</label>
                    <input type="email" name="email" value={formData.email} onChange={handleChange} />
                    {errors.email && <p className="error">{errors.email}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.label.password')}</label>
                    <input type="password" name="passwordHash" value={formData.passwordHash} onChange={handleChange} />
                    {errors.passwordHash && <p className="error">{errors.passwordHash}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.label.firstName')}</label>
                    <input type="text" name="firstName" value={formData.firstName} onChange={handleChange} />
                    {errors.firstName && <p className="error">{errors.firstName}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.label.lastName')}</label>
                    <input type="text" name="lastName" value={formData.lastName} onChange={handleChange} />
                    {errors.lastName && <p className="error">{errors.lastName}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.label.phone')}</label>
                    <input type="text" name="phone" value={formData.phone} onChange={handleChange} />
                    {errors.phone && <p className="error">{errors.phone}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.label.address')}</label>
                    <input type="text" name="address" value={formData.address} onChange={handleChange} />
                    {errors.address && <p className="error">{errors.address}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.label.postalCode')}</label>
                    <input type="text" name="postalCode" value={formData.postalCode} onChange={handleChange} />
                    {errors.postalCode && <p className="error">{errors.postalCode}</p>}
                </div>
                <div className="form-group">
                    <label>{t('registerform.label.city')}</label>
                    <input type="text" name="city" value={formData.city} onChange={handleChange} />
                    {errors.city && <p className="error">{errors.city}</p>}
                </div>

                {errors.submit && <p className="error">{errors.submit}</p>}

                <button type="submit">{t('registerform.button.submit')}</button>
            </form>
        </div>
    );
};

export default RegisterForm;
