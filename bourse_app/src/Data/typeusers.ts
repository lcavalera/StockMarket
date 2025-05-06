// src/typeUsers.ts
export interface UserProfile {
    id: number
    userName: string;
    password: string;
    firstName: string;
    lastName: string;
    phone: string;
    role: string; // Par défaut, l'utilisateur est "Public"
    address: string;
    postalCode: string;
    city: string;
    }
    