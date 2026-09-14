# Concept
Une application qui permet à un utilisateur d'obtenir une liste de voitures en fonction de différents critères. Quand il choisira une voiture, il aura le détail complet de la voiture.  

# Liste des critères
L'utilisateur pourra activer et désactiver autant de critères qu'ils souhaitent.  
- budget : mini et/ou max  
- marque  
- catégorie  
    - berline  
    - compacte  
    - S.U.V.  
    - etc ...    
- technologie :   
    - essence  
    - gasoil  
    - électrique  
    - gaz  
    - éthanol  
    - hybride  
    - autre ?  
- nombre de places : 2, 3, 4, 5, 6, 7, 8, 9
- volume de coffre  
- autonomie sur route en nombre de kilomètres  
- pour la technologie électrique :  
    - durée de charge de 0 à 80%  
    - durée de charge complète  
- les options :  
    - rétroviseurs extérieurs rabattable automatiquement  
    - boite mécanique / automatique  
    - toit ouvrant  
    - toit panoramique  
    - toit panoramique occultant  
    - siège chauffant  
    - mémorisation de la position du siège  
    - siège réglable chauffeur  
    - siège réglable passager  
    - etc...  

# Tri de la liste
La liste qui apparaît à l'utilisateur, fonction des critères, doit pouvoir être trier.  
Les critères de tri : 
- le tarif  
- le nombre de like  
- le nombre de vues  

# Information sur une voiture
Tous les critères vus ci-dessus. On y ajoutera :  
- des visuels (visuel principal avec miniature,face avant, face arrière, profil gauche, profil droit, autre)  
- la marque du véhicule  
- le modèle  
- un lien http vers la fiche web du constructeur  

# fonctionnalités du site
- l'utilisateur doit pouvoir liker un véhicule (de 1 à 5 étoiles)  
- il doit pouvoir laisser un commentaire  
- pour chaque fiche de véhicule visionnée, on maintiendra un compteur à jour du nombre de vues  
- on mesurera la durée de visite pour le site au global, chaque fiche, nombre de visites par utilisateur, etc ...
- le site sera utilisable sur navigateur d'ordinateur mais aussi sur smartphone  
- prévoir le popup pour l'acceptation ou non des cookies. Le site sera utilisable sans cookie.  
- s'il y a des cookies, on stockera sur le navigateur de l'utilisateur les informations de filtres, tris, ...  
- le site aura une ergonomie actuelle, moderne, simple  

# Fonctionnalités d'administration
- création et gestion de comptes  
- un super admin qui valide la création de compte  
- des rôles : super admin, créateur de fiches voiture, valideur de fiches voiture  

# Application web
- langage dotnet C# dernière version  
- technologie blazor  
- base de données

# Outils
- Parseur de site web des constructeurs automobile pour créer automatiquement des fiches voitures  


