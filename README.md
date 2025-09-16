# 🎯 CTSAR-Booking

Application de gestion des réservations d'alvéoles pour club de tir sportif.

## 📋 Description

Application **Blazor Web App** (.NET 8) permettant la gestion complète des réservations d'alvéoles dans un club de tir. Le système assure qu'aucune séance de tir ne peut avoir lieu sans la validation d'un moniteur qualifié.

### Fonctionnalités principales

- 📅 **Planning mensuel** interactif et temps réel
- 👥 **Gestion des membres** avec rôles (Membre, Moniteur, Administrateur)
- 🎯 **Réservation d'alvéoles** avec validation obligatoire par moniteur
- 🔔 **Notifications modulaires** (Email, WhatsApp)
- 🌍 **Multi-language** (Français, Allemand, Anglais)
- 🚫 **Gestion des fermetures** d'alvéoles (maintenance, jours fériés)

## 🏗️ Architecture

- **Framework :** Blazor Web App (.NET 8)
- **Architecture :** Vertical Slice simplifiée
- **Base de données :** Entity Framework Core + SQLite (dev) / PostgreSQL (prod)
- **Modes de rendu :** Server/WebAssembly/Static selon les besoins

## 🚀 Démarrage rapide

### Prérequis

- .NET 8 SDK
- Visual Studio 2022 ou VS Code
- Git

### Installation

```bash
# Cloner le repository
git clone https://github.com/votre-username/CTSAR-booking.git
cd CTSAR-booking

# Restaurer les dépendances
dotnet restore

# Lancer l'application
cd src/CTSAR.Booking/CTSAR.Booking
dotnet run
```

L'application sera disponible sur `https://localhost:7041`

### Comptes de test

- **Administrateur :** jean.martin@club-tir.fr
- **Moniteur :** marie.dubois@club-tir.fr

## 📦 Déploiement

### Conteneurisation

```bash
# Construire l'image Docker
docker build -t ctsar-booking .

# Lancer le conteneur
docker-compose up -d
```

## 🗃️ Structure du projet

```
CTSAR-Booking/
├── src/
│   └── CTSAR.Booking/
│       ├── CTSAR.Booking/          # Projet principal Blazor Server
│       └── CTSAR.Booking.Client/   # Composants WebAssembly
├── Models/                         # Entités de données
├── Data/                          # DbContext et configurations EF
├── Features/                      # Organisation par fonctionnalité
└── Components/                    # Composants Blazor réutilisables
```

## 🛠️ Développement

### Modèles de données

- `Membre` : Gestion des utilisateurs et rôles
- `Alveole` : Configuration des stands de tir
- `Reservation` : Réservations avec validation moniteur
- `PeriodeFermeture` : Gestion des fermetures temporaires

### Commandes utiles

```bash
# Ajouter une migration
dotnet ef migrations add NomMigration

# Mettre à jour la base de données
dotnet ef database update

# Lancer les tests
dotnet test
```

## 📄 Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.

## 🤝 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à ouvrir une issue ou soumettre une pull request.

---

**État du projet :** 🔄 En développement actif

- ✅ Modèles de données et base
- ✅ Architecture et structure
- 🔄 Interface utilisateur
- ⏳ Authentification et autorisations
- ⏳ Système de notifications