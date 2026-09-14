# ChooseYourCar - Journal des décisions

Ce document conserve les décisions et orientations prises pendant le cadrage du projet. Les éléments marqués `Décision` sont validés. Les éléments marqués `À définir` restent ouverts.

## Historique

### 2026-09-14 - Cadrage fonctionnel et technique

- `Décision` : le produit est une application web de recherche et de consultation de fiches de véhicules neufs.
- `Décision` : le marché initial est la France.
- `Décision` : les constructeurs ciblés sont Renault, Peugeot, Citroën, Tesla et BYD.
- `Décision` : environ 50 véhicules variés serviront de jeu de données de test ; le volume est réparti progressivement, en commençant par Tesla puis Renault et d'autres marques.
- `Décision` : la mise à jour des données est déclenchée à la demande de l'administrateur.
- `Décision` : une saisie manuelle par l'administrateur est disponible comme solution de secours.
- `Décision` : le prototype de scraping commence avec Tesla, puis pourra être étendu aux autres constructeurs.
- `Décision` : les modèles Tesla ciblés sont Model Y, Model 3, Model S et Model X.
- `Décision` : l'URL de départ du prototype est https://www.tesla.com/fr_fr/.
- `Décision` : le scraping Tesla est une première version exploratoire hors production, destinée à mesurer les données réellement récupérables.
- `Décision` : le prototype ne cherche pas à copier l'intégralité du site ; il cible les champs nécessaires aux filtres et aux fiches.
- `À définir` : les conditions d'utilisation et les droits sur les contenus seront vérifiés avant la production.

## Règles métier validées

- `Décision` : la consultation des véhicules publiés est libre.
- `Décision` : un compte avec adresse e-mail vérifiée est obligatoire pour commenter.
- `Décision` : les utilisateurs bannis ou suspendus sont gérés par l'application.
- `Décision` : tout commentaire est modéré avant publication.
- `Décision` : favoris et notes de 1 à 5 sont deux fonctionnalités séparées.
- `Décision` : tous les critères actifs sont combinés avec `ET`.
- `Décision` : les prix concernent les véhicules neufs et sont disponibles en HT et TTC.
- `Décision` : le volume du coffre est exprimé en litres.
- `Décision` : l'autonomie électrique est exprimée selon WLTP.
- `Décision` : les résultats sont paginés progressivement avec un bouton pour continuer la liste.
- `Décision` : les fiches comprennent notamment l'année, la puissance, la transmission, les émissions de CO2, la génération, la finition et la date de dernière mise à jour.
- `Décision` : une donnée absente est conservée comme inconnue ou `null`, jamais comme zéro.
- `Décision` : une donnée contradictoire est inscrite dans un journal d'import associé à la fiche.
- `Décision` : le journal d'import conserve les valeurs sources, la source, la date, le champ concerné, la gravité et la décision de résolution ; il est visible avant validation.

## Administration et workflow

- `Décision` : les rôles sont super administrateur, créateur de fiches et valideur de fiches.
- `Décision` : le workflow est `Brouillon -> En attente de validation -> Publié -> Archivé`.
- `Décision` : une fiche non publiée est invisible au public.
- `Décision` : le créateur ne peut pas publier directement une fiche qu'il a créée.
- `Décision` : l'administration permet de gérer les comptes, les fiches, les imports et les anomalies.

## Architecture validée

- `Décision` : application en C# avec la dernière version .NET disponible au démarrage.
- `Décision` : interface Blazor.
- `Décision` : base de données SQLite.
- `Décision` : accès aux données avec Entity Framework Core.
- `Décision` : authentification avec ASP.NET Core Identity.

## Confidentialité, statistiques et publicité

- `Décision` : les statistiques sont anonymisées et agrégées uniquement.
- `Décision` : les indicateurs comprennent visites, visiteurs uniques anonymisés, vues par fiche, durée moyenne, taux de retour, favoris, notes, clics vers le constructeur et taux de conversion vers ces liens.
- `Décision` : l'application applique les recommandations RGPD : consentement, information, minimisation, durée de conservation, suppression et anonymisation.
- `Décision` : le site reste utilisable sans cookies non essentiels.
- `Décision` : des emplacements publicitaires sont prévus et leur gestion sera accessible à l'administration.
- `À définir` : formats, régies, emplacements précis, campagnes, périodes de diffusion et statistiques publicitaires.

## MVP et suite

- `Décision` : le MVP comprend catalogue validé, filtres, tri, pagination, fiches, responsive, administration, rôles, Identity, favoris, notes, commentaires modérés, consentement RGPD, emplacements publicitaires réservés et prototype Tesla.
- `Décision` : la version suivante pourra ajouter les parseurs Renault et autres constructeurs, l'industrialisation des imports, les campagnes publicitaires, la comparaison de véhicules, les alertes et le partage de recherches.

## Questions restantes

1. `À définir` : service d'envoi des e-mails pour la vérification Identity et les notifications.
2. `À définir` : hébergement et séparation des environnements développement, test et production.
3. `À définir` : règles détaillées pour les données absentes ou contradictoires lors de la saisie manuelle.
4. `À définir` : formats et emplacements publicitaires lorsque cette fonctionnalité sera priorisée.
5. `À définir` : validation des conditions d'utilisation et des droits Tesla avant production.

## Références projet

- Cahier des charges : [Specifications.md](Specifications.md)
- Source initiale du prototype Tesla : https://www.tesla.com/fr_fr/
