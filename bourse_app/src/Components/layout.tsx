// src/components/Layout.tsx
import React from 'react';
import { Outlet } from 'react-router-dom';
import Header from '../Components/header.tsx';
import Footer from '../Components/footer.tsx';
// import '../Css/layout.css'; // Fichier CSS spécifique au layout

const Layout: React.FC = () => {
    // const [refreshCount, setRefreshCount] = useState(0);

    // Fonction pour rafraîchir le Header
    // const refreshHeader = () => {
    //     console.log('test')
    //     setRefreshCount(prevCount => prevCount + 1); // Modifie l'état pour déclencher un rerender
    // };

    return (
        <div id='layout' className="layout">
            {/* <RefreshProvider onRefresh={refreshHeader}> */}
                {/* <Header refreshCount={refreshCount}></Header> */}
                <Header></Header>
                <main className="main">
                    <Outlet /> {/* Displays the current route component */}
                </main>
                <Footer></Footer>
            {/* </RefreshProvider> */}
        </div>
    );
};

export default Layout;
