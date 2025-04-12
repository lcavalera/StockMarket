import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import React from 'react';

interface FooterProps {
  // You can add props here if needed
}

const Footer: React.FC<FooterProps> = () => {
  const { t } = useTranslation();

  return (
    <footer className="footer">
      <div className="footer-content">
      <div className="footer-block">
          <p>{t('footer.title')}</p>
        </div>
        <div className="footer-block">
          <ul>
            <li><Link to="/rights">{t('footer.bloc1.link2')}</Link></li>
          </ul>
        </div>
        <div className="footer-block">
          <ul>
            <li><Link to="/faq">{t('footer.bloc2.link2')}</Link></li>
          </ul>
        </div>
        <div className="footer-block">
          <ul>
            <li><a href="https://ecolewahta.wendake.ca/nous-joindre/#form" target="blank">{t('footer.bloc3.link1')}</a></li>
          </ul>
        </div>
        <div className="footer-block">
          {/* <a href="https://ecolewahta.wendake.ca" target="blank"><img className="logo-footer-watha" src="../logo-wendake-blue.png" alt="logo wahta" /></a>
          <a href="https://wendake.ca" target='blank'><img className="logo-footer-wendake" src="../LogoWendake.png" alt="logo conseil de bande" /></a> */}
        </div>
        <div></div><div></div>
      </div>
    </footer>
  );
};

export default Footer;