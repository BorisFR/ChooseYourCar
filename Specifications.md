# ChooseYourCar - Cahier des charges

## 1. Objectif

ChooseYourCar est une application web qui permet de rechercher des voitures selon plusieurs critères, de consulter une fiche détaillée et de comparer la popularité des véhicules. Le lancement cible le marché français et les véhicules neufs.

Le premier jeu de données comportera environ 50 véhicules variés pour tester l'application. Il commencera avec quelques modèles Tesla, puis sera complété avec Renault et d'autres constructeurs.

Constructeurs prévus : Renault, Peugeot, Citroën, Tesla et BYD.

## 2. Parcours utilisateur

### Visiteur

- consulter les véhicules publiés sans créer de compte ;
- rechercher et filtrer les véhicules ;
- trier et parcourir les résultats par pagination progressive ;
- consulter une fiche détaillée et ouvrir le lien du constructeur ;
- consulter les notes et commentaires publiés.

### Utilisateur authentifié

- disposer d'une adresse e-mail vérifiée ;
- ajouter ou retirer un véhicule de ses favoris ;
- attribuer une note de 1 à 5 ;
- laisser un commentaire soumis à modération ;
- gérer ses informations et demander la suppression de son compte.

Un utilisateur peut être suspendu ou banni. Les règles d'accès après suspension ou bannissement seront appliquées par ASP.NET Core Identity et les autorisations de l'application.

## 3. Recherche et filtres

Les critères actifs sont combinés avec l'opérateur **ET**. Un critère non sélectionné ne restreint pas les résultats. Une donnée absente est inconnue et ne doit jamais être affichée comme zéro.

Critères :

- prix minimum et maximum, en HT et TTC ;
- marque ;
- catégorie : berline, compacte, SUV, etc. ;
- technologie : essence, diesel, électrique, gaz, éthanol, hybride, autre ;
- année ;
- génération ;
- finition ;
- nombre de places ;
- volume du coffre en litres ;
- autonomie électrique selon WLTP ;
- puissance ;
- transmission : mécanique ou automatique ;
- émissions de CO2 ;
- durée de charge de 0 à 80 % ;
- durée de charge complète ;
- équipements et options, notamment rétroviseurs rabattables, toit ouvrant ou panoramique, sièges chauffants et réglages/mémorisation des sièges.

Si aucun véhicule ne correspond, l'application affiche un message explicite et permet de modifier les critères.

Les résultats sont chargés progressivement avec un bouton permettant de continuer la liste. La pagination conserve les filtres et le tri courants.

## 4. Tri

La liste peut être triée par :

- prix ;
- note moyenne et nombre de notes ;
- nombre de favoris ;
- nombre de vues ;
- date de dernière mise à jour.

Les favoris et les notes de 1 à 5 sont deux fonctionnalités distinctes.

## 5. Fiche véhicule

Une fiche publiée contient, lorsque les données sont disponibles :

- marque, modèle, génération, finition et année ;
- motorisation, technologie, puissance et transmission ;
- prix HT et TTC ;
- nombre de places et volume du coffre ;
- autonomie WLTP et informations de recharge ;
- émissions de CO2 ;
- équipements et options ;
- visuel principal et visuels supplémentaires : avant, arrière, profils et autres ;
- URL officielle du constructeur ;
- date de collecte et date de dernière mise à jour ;
- note moyenne, nombre de notes, favoris et vues ;
- commentaires publiés.

Une information indisponible est affichée comme « inconnue » ou masquée selon le contexte, mais jamais comme une valeur numérique nulle.

## 6. Commentaires et modération

- le compte et l'adresse e-mail doivent être vérifiés pour commenter ;
- tout commentaire est soumis à modération avant publication ;
- les commentaires peuvent être acceptés, rejetés, masqués ou supprimés ;
- les utilisateurs peuvent signaler un commentaire ;
- les actions de modération sont journalisées.

## 7. Administration

### Rôles

- **Super administrateur** : gestion des comptes, rôles, paramètres, véhicules et publications ;
- **Créateur de fiches** : création et modification des fiches, consultation des journaux d'import ;
- **Valideur de fiches** : contrôle des données, résolution des anomalies et publication.

### Workflow des fiches

`Brouillon -> En attente de validation -> Publié -> Archivé`

Une fiche en brouillon ou en attente n'est pas visible par le public. Le créateur ne peut pas publier directement une fiche qu'il a créée.

### Gestion des données

L'administrateur peut déclencher une mise à jour à la demande et saisir ou corriger manuellement une fiche lorsque l'import automatique est incomplet.

Les données contradictoires sont enregistrées dans un journal d'import associé à la fiche. Le journal conserve les valeurs sources, la date, la source, le champ concerné, le niveau de gravité et la décision de résolution. Il est visible par le créateur et le valideur avant publication.

## 8. Données et prototype Tesla

Le premier parseur est un prototype exploratoire hors production. Il teste les modèles suivants à partir de [tesla.com/fr_fr](https://www.tesla.com/fr_fr/) :

- Model Y ;
- Model 3 ;
- Model S ;
- Model X.

Le prototype doit mesurer ce qui est réellement récupérable et produire un rapport de couverture par champ : récupérable, partiellement récupérable, absent ou à saisir manuellement.

Il cible les données nécessaires aux filtres et aux fiches : identité du véhicule, version, prix, motorisation, caractéristiques, équipements, autonomie WLTP, recharge, émissions, visuels, URL officielle et dates de collecte. Il ne cherche pas à copier l'intégralité du site.

Les conditions d'utilisation et les droits sur les textes et images seront vérifiés avant toute exploitation en production. La saisie manuelle reste le mécanisme de secours.

## 9. Statistiques et confidentialité

Les statistiques sont anonymisées et agrégées uniquement. Les indicateurs suivis sont :

- visites ;
- visiteurs uniques anonymisés ;
- vues par fiche ;
- durée moyenne de consultation ;
- taux de retour ;
- favoris ;
- notes ;
- clics vers le site constructeur ;
- taux de conversion vers les liens constructeurs.

L'application doit respecter le RGPD : consentement, information des utilisateurs, minimisation des données, durée de conservation, suppression des comptes et anonymisation des statistiques. Le site reste utilisable sans cookies non essentiels.

Les préférences de filtres et de tri peuvent être conservées localement dans le navigateur avec le consentement approprié.

## 10. Publicité

Des emplacements réservés sont prévus sur les pages du site et doivent pouvoir être administrés. Le choix des formats, régies, emplacements définitifs, périodes de diffusion et indicateurs publicitaires est reporté.

La gestion devra à terme permettre d'activer ou désactiver un emplacement ou une campagne et de contrôler les contenus affichés. Les impressions et clics devront rester anonymisés et respecter le consentement RGPD.

## 11. Exigences non fonctionnelles

- application responsive sur ordinateur et smartphone ;
- interface moderne, simple et accessible ;
- séparation des environnements développement, test et production ;
- sauvegardes de la base et journalisation des opérations importantes ;
- contrôle des autorisations pour chaque rôle ;
- tests unitaires des règles de filtrage et tests d'intégration des imports ;
- traitement explicite des erreurs d'import et des données absentes.

## 12. Architecture technique

- langage : C# avec la dernière version .NET disponible au démarrage ;
- interface : Blazor ;
- authentification et comptes : ASP.NET Core Identity ;
- base de données : SQLite ;
- accès aux données : Entity Framework Core ;
- parseurs de constructeurs : composants isolés, avec saisie manuelle de secours.

Le service d'envoi des e-mails de vérification et l'hébergement seront définis avant la mise en production.

## 13. MVP

Le MVP comprend :

- catalogue de véhicules validés ;
- filtres, tri et pagination progressive ;
- fiches détaillées ;
- responsive ordinateur/mobile ;
- administration des fiches et des comptes ;
- rôles et workflow de validation ;
- ASP.NET Core Identity avec vérification e-mail ;
- favoris et notes ;
- commentaires modérés ;
- consentement RGPD ;
- emplacements publicitaires réservés ;
- prototype Tesla et import manuel de secours.

## 14. Version suivante

- parseurs Renault puis autres constructeurs ;
- industrialisation des imports ;
- gestion complète des campagnes publicitaires ;
- modération et statistiques avancées ;
- comparaison de plusieurs véhicules ;
- alertes et partage de recherches.

## 15. Critères d'acceptation

- une recherche sans critère retourne les véhicules publiés ;
- les critères actifs sont combinés avec ET ;
- un résultat vide est clairement signalé ;
- une donnée absente n'est jamais affichée comme zéro ;
- une contradiction apparaît dans le journal d'import associé à la fiche ;
- une fiche en brouillon ou en attente est invisible au public ;
- un créateur ne peut pas publier directement sa fiche ;
- un utilisateur doit avoir une adresse e-mail vérifiée pour commenter ;
- un commentaire n'est visible publiquement qu'après modération ;
- un utilisateur suspendu ou banni ne peut pas effectuer les actions interdites ;
- le refus des cookies non essentiels n'empêche pas la consultation ;
- les filtres, le tri et le bouton de continuation fonctionnent sur mobile ;
- un import échoué est journalisé et signalé à l'administration ;
- les statistiques ne permettent pas d'identifier directement un utilisateur.


