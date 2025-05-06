// import { useAuth } from "../Auth/autoContext";

export default function AbonnementsPage() {
  // const { login } = useAuth();

  return (
    <div>
      <h1>Choisissez votre abonnement</h1>
      {/* <button onClick={() => login('premium')}>Passer Premium</button> */}
      <button>Passer Premium</button>
      <p>Accès Free : Liste simple sans inscription.<br />
         Accès Free+ : Historique après inscription.<br />
         Accès Premium : Accès complet à toutes les fonctionnalités.
      </p>
    </div>
  );
}
