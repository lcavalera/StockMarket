// src/components/ui/button.tsx

import React from 'react';

interface ButtonProps {
  onClick: () => void;
  label?: string;  // Le texte qui sera affiché sur le bouton
  disabled?: boolean;  // Ajout de l'option disabled
  children?: React.ReactNode; // Ajout de children pour gérer le contenu dynamique du bouton
}

export const Button: React.FC<ButtonProps> = ({ onClick, label, disabled = false, children  }) => {
  return (
    <button
      onClick={onClick}
      className='button-pagination'
      disabled={disabled}  // Application du disabled
    >
       {children || label}  {/* Affichage du label ou du contenu passé dans children */}
    </button>
  );
};