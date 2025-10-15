# 🔐 IDENTIFIANTS DE TEST - CTSAR BOOKING

**Ces identifiants sont créés automatiquement au démarrage de l'application.**

---

## 👤 ADMINISTRATEUR

**Email** : `admin@ctsar.fr`
**Mot de passe** : `Admin123!`
**Rôle** : Administrateur

**Permissions** :
- ✅ Accès total à l'application
- ✅ Gestion des utilisateurs
- ✅ Gestion de la configuration
- ✅ Toutes les fonctionnalités

---

## 🎯 MONITEURS (2 comptes)

### Moniteur 1
**Email** : `moniteur1@ctsar.fr`
**Mot de passe** : `Monitor123!`
**Rôle** : Moniteur

### Moniteur 2
**Email** : `moniteur2@ctsar.fr`
**Mot de passe** : `Monitor123!`
**Rôle** : Moniteur

**Permissions** :
- ✅ Valider/annuler leur présence sur des créneaux
- ✅ S'inscrire aux créneaux (comme un membre)
- ✅ Voir toutes les réservations

---

## 👥 MEMBRES (5 comptes)

### Membre 1 - Jean Dupont
**Email** : `membre1@ctsar.fr`
**Mot de passe** : `Membre123!`
**Rôle** : Membre

### Membre 2 - Marie Martin
**Email** : `membre2@ctsar.fr`
**Mot de passe** : `Membre123!`
**Rôle** : Membre

### Membre 3 - Pierre Bernard
**Email** : `membre3@ctsar.fr`
**Mot de passe** : `Membre123!`
**Rôle** : Membre

### Membre 4 - Sophie Dubois
**Email** : `membre4@ctsar.fr`
**Mot de passe** : `Membre123!`
**Rôle** : Membre

### Membre 5 - Luc Lefebvre
**Email** : `membre5@ctsar.fr`
**Mot de passe** : `Membre123!`
**Rôle** : Membre

**Permissions** :
- ✅ S'inscrire aux créneaux de tir
- ✅ Se désinscrire des créneaux
- ✅ Voir le planning
- ✅ Modifier son profil

---

## ⚠️ IMPORTANT - SÉCURITÉ

**CES MOTS DE PASSE SONT UNIQUEMENT POUR LE DÉVELOPPEMENT !**

En production :
1. ❌ Ne jamais utiliser ces mots de passe simples
2. ✅ Utiliser des mots de passe forts (12+ caractères, majuscules, minuscules, chiffres, symboles)
3. ✅ Activer la confirmation par email
4. ✅ Activer l'authentification à deux facteurs (2FA)
5. ✅ Supprimer les comptes de test

---

## 🚀 POUR TESTER

1. Lancer l'application : `dotnet watch run`
2. Ouvrir le navigateur : `https://localhost:5001` (ou le port affiché)
3. Cliquer sur "Log in" ou "Se connecter"
4. Utiliser un des identifiants ci-dessus

**URL de connexion** : `/Account/Login`

---

## 📝 NOTES

- Les mots de passe sont configurés dans `Data/DbInitializer.cs`
- Pour changer les exigences de mot de passe : voir `Program.cs` lignes 76-81
- La base de données SQLite est stockée dans `app.db`
- Pour réinitialiser : supprimer `app.db` et relancer l'app
