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
      <FaGlobe
        onClick={() => setIsOpen(!isOpen)} // Basculer l'état du menu
        style={{ cursor: 'pointer', fontSize: '30px' }}
      />
      
      {/* Menu avec les options FR/EN, visible quand isOpen est true */}
      {isOpen && (
        <div 
          style={{
            position: 'absolute',
            top: '40px', // Positionner le menu sous l'icône
            backgroundColor: '#fff',
            border: '1px solid #ccc',
            padding: '10px',
            borderRadius: '5px',
            boxShadow: '0px 4px 6px rgba(0, 0, 0, 0.1)',
          }}
        >
          <div 
            style={{
              cursor: 'pointer', 
              padding: '5px',
              transition: 'background-color 0.3s', // Transition pour l'effet de survol
            }} 
            onClick={() => handleLanguageChange('fr')}
            onMouseEnter={(e) => [e.currentTarget.style.backgroundColor = '#5a5a5a', e.currentTarget.style.color = '#f0f0f0']} // Survol
            onMouseLeave={(e) => [e.currentTarget.style.backgroundColor = 'transparent', e.currentTarget.style.color = '#221F20']} // Retirer l'effet
          >
            FR
          </div>
          <div 
            style={{
              cursor: 'pointer', 
              padding: '5px',
              transition: 'background-color 0.3s', // Transition pour l'effet de survol
            }} 
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
