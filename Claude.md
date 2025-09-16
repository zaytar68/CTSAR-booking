# Claude.md

## Project overview

Application **Blazor Web App** (.NET 8) de gestion de réservations d'alvéoles pour club de tir. Architecture **Vertical Slice** simplifiée pour un développement modulaire et évolutif.

**Principe :** Une séance de tir doit obligatoirement être validée par la présence d'un moniteur.
- Tout le monde peut voir la présence de tous les membres et moniteurs
- Un ou plusieurs membres peuvent s'inscrire sur une réservation
- Un moniteur peut valider une ou plusieurs réservations
- Interface différenciée visuellement selon le statut (confirmée/en attente)

## Fonctionalités

- Affichage d'un planning mensuel avec une interface très conviviale et simple d'utilisation.
- Gestion des membres avec authentification, autorisation et rôles.
- Interface de gestion des membres.
- Gérer la disponibilité des alvéoles. Possibilité de les fermer (travaux, jour férié, réservations extérieures, etc.)
- Possibilité pour un moniteur d'annuler sa présence.
- Sauvegarder la préférence de langue par utilisateur.

## Notifications

- prévoir un système modulaire ou les membres peuvent s'abonner à plusieurs systèmes de notification ( mail, whatsapp, etc.).
- Notifier les membres inscrits à la réservation lorsqu'un moniteur valide sa présence.
- Notifier les membres inscrits à la réservation lorsqu'un moniteur annule sa présence.

## Page de configuration (accessible Administrateurs)

- Réglage des horaires d'ouverture chaque jour de la semaine (Par défaut du lundi au samedi 8h00-12h00 14h00-17h00 et le dimanche 8h00-12h00)

## Development Guidelines

- Demander l'utilisateur (Human in the loop) si il y a des questions sur le fonctionnement.
- L'application doit supporter le multi-language (Français par défaut, allemand, anglais)
- L'application doit être modulaire afin d'y ajouter facilement des fonctionnalités.
- Toujours utiliser des noms de variables descriptives (Always use descriptive variable names)
- Follow existing validation patterns using Data Annotations
- use context7
- génère et met à jour une todo list pour suivre l'avancement du projet
- utiliser github pour suivre le développement

## Modèles

- role : Administrateur, Membre, Moniteur
- alveole : nom
- membre : nom, prenom, mail, role
- reservation : date, heure début, heure fin, membres inscrits, moniteurs inscrits

## Nice to have

- notification Whatsapp
- Module météo

## Considérations techniques

### Stack Technique
- **Framework :** Blazor Web App (.NET 8) avec modes hybrides (Server/WebAssembly/Static)
- **Base de données :** SQLite (dev) → PostgreSQL/SQL Server (prod)
- **ORM :** Entity Framework Core 9.0
- **Architecture :** Vertical Slice simplifiée
- **Déploiement :** Conteneurisé sous Linux (Docker)

### État du Projet
- ✅ **Phase 1 Terminée** : Structure projet + modèles de données (Membre, Alveole, Reservation)
- ✅ **Base de données** : Configuration EF Core + données de test
- 🔄 **Phase 2 en cours** : Interface utilisateur + fonctionnalités de base
- ⏳ **À venir** : Authentification + notifications + containerisation

### Modèles Implémentés
```csharp
// Entités principales créées
public class Membre     // Gestion des utilisateurs (Administrateur, Moniteur, Membre)
public class Alveole    // Stands de tir avec disponibilité
public class Reservation // Réservations avec validation moniteur
public class PeriodeFermeture // Fermetures temporaires des alvéoles
```

### Prochaines Étapes
1. Créer l'interface de planning mensuel
2. Implémenter le système d'authentification
3. Développer les notifications modulaires
4. Préparation du déploiement conteneurisé
- Use responsive design pour affichage compatible sur smartphone