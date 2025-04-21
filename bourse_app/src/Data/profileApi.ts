// src/data/profileApi.ts
import axios from 'axios';
import bcrypt from 'bcryptjs';

// import { getUser, login, logout, completeLogin} from '../Data/authClientService';

interface UserProfile {
  id: number;
  email: string;
  passwordHash: string;
  firstName: string;
  lastName: string;
  phone: string;
  role: string;
  address: string;
  postalCode: string;
  city: string;
}

const API_BASE_URL = 'https://localhost:7157/api'; // Remplacez par l'URL de votre API

const httpclient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 3000
})

httpclient.defaults.headers.post['Content-Type'] = 'application/json'

//On recupere le token pour chaque requete à l'Api
const token = sessionStorage.getItem('token');
httpclient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

interface LoginResponse {
  token: string;
  user: UserProfile;
}

//Methode pour stocker le profile utilisateur
const saveUserTosessionStorage = (user: UserProfile) => {
  const userJSON = JSON.stringify(user);
  sessionStorage.setItem('user', userJSON);
};

// Methode pour recuperer le profile utilisateur stocké
export const getUserFromSessionStorage = () => {
  const userJSON = sessionStorage.getItem('user');
  if (userJSON) {
    return JSON.parse(userJSON);
  }
  return null;
};

export const loginApi = async (profileData: any) => {
  try {

    // Appel à l'API pour la connexion
    const response = await httpclient.post<LoginResponse>('Users/Login', profileData);

    if (response.status !== 200) {
      throw new Error('La connexion a échoué.');
    };

    const isLogin = await bcrypt.compare(profileData.password, response.data.user.passwordHash);
    
    if(isLogin){
      console.log('Connexion réussie', response.data);
      saveUserTosessionStorage(response.data.user);
      // Save the token or navigate to another page
      sessionStorage.setItem('token', response.data.token);

      // sessionStorage.setItem('username', response.data.user.firstName);

      return response.data;
    }
    else{
      throw new Error('La connexion a échoué. Password incorrect.');
    }

  } catch (error) {
    console.error('Error during login:', error);
    throw new Error('La connexion a échoué. Veuillez réessayer.');
  }
};

// Fonction pour récupérer le profil utilisateur après la connexion
export const fetchUserProfile = async (token: string): Promise<UserProfile> => {
  const response = await axios.get<UserProfile>('/api/profile', {
    headers: {
      Authorization: `Bearer ${token}` // Utiliser le token d'authentification
    }
  });
  return response.data;
};

export const getProfiles = async () => {
  try {
    const response = await httpclient.get<UserProfile[]>('Users');
    return response.data;
  } catch (error) {
    throw new Error('Erreur de chargement du profil');
  }
};

export const updateProfile = async (profileData: any) => {
  try {
    console.log(profileData);
    const response = await httpclient.put('Users/' + profileData.id, profileData);
    if(response.status === 204){
      saveUserTosessionStorage(profileData);
    }

    console.log(response);
    return response.data;
  } catch (error) {
    throw new Error('Erreur de sauvegarde du profil');
  }
};

export const addProfile = async (profileData: any) => {
  try {
    console.log(profileData);
    const response = await httpclient.post('Users', profileData);
    return response.data;

  } catch (error: any) {
    if(error.response.data.error){
      throw new Error(error.response.data.error);
    }
    else{
      throw new Error("Erreur d'ajout du profil");
    }
  }
};
