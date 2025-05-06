import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FaGlobe } from 'react-icons/fa'; // Utilisation de l'icône "Globe" de react-icons

const LanguageDropdown: React.FC = () => {
  const { i18n } = useTranslation();
  const [isOpen, setIsOpen] = useState(false); // Pour contrôler l'état du menu déroulant

  const handleLanguageChange = (language: string) => {
    i18n.changeLanguage(language);
    sessionStorage.setItem('language', language);
    setIsOpen(false); // Fermer le menu après la sélection
  };

  return (
    <div style={{ position: 'relative' }}>
      {/* Icône du globe, au clic elle ouvre/ferme le menu */}
      <FaGlobe className='globe'
        onClick={() => setIsOpen(!isOpen)} // Basculer l'état du menu
      />
      
      {/* Menu avec les options FR/EN, visible quand isOpen est true */}
      {isOpen && (
        <div className='container-langues'>
          <div className='langue'
            onClick={() => handleLanguageChange('fr')}
            onMouseEnter={(e) => [e.currentTarget.style.backgroundColor = '#5a5a5a', e.currentTarget.style.color = '#f0f0f0']} // Survol
            onMouseLeave={(e) => [e.currentTarget.style.backgroundColor = 'transparent', e.currentTarget.style.color = '#221F20']} // Retirer l'effet
          >
            FR
          </div>
          <div className='langue'
            onClick={() => handleLanguageChange('en')}
            onMouseEnter={(e) => [e.currentTarget.style.backgroundColor = '#5a5a5a', e.currentTarget.style.color = '#f0f0f0']} // Survol
            onMouseLeave={(e) => [e.currentTarget.style.backgroundColor = 'transparent', e.currentTarget.style.color = '#221F20']} // Retirer l'effet
          >
            EN
          </div>
        </div>
      )}
    </div>
  );
};

export default LanguageDropdown;
