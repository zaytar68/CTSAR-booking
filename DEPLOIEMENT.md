# Guide de Déploiement CTSAR-Booking

## Architecture

- **Reverse Proxy** : Traefik v3.6 avec SSL Let's Encrypt automatique
- **Application** : Blazor .NET 8 (conteneur Docker)
- **Base de données** : SQLite avec volume persistant
- **Orchestration** : Portainer
- **CI/CD** : GitHub Actions (build automatique sur branche `production`)

---

## 1. Traefik (Reverse Proxy + SSL)

### Déploiement dans Portainer

1. **Créer un nouveau stack** :
   - Nom : `traefik`
   - Contenu : copier le fichier [traefik-portainer.yml](traefik-portainer.yml)

2. **Déployer le stack**

3. **Vérifications** :
   - Dashboard accessible : http://192.168.1.253:8080/dashboard/
   - Ports exposés : 80 (HTTP), 443 (HTTPS), 8080 (Dashboard)
   - Réseau créé : `traefik_default`

### Caractéristiques

- **Dashboard** : Accessible uniquement depuis le réseau local (192.168.1.0/24)
- **SSL automatique** : Let's Encrypt avec challenge HTTP
- **Redirection HTTP → HTTPS** : Automatique pour tous les services
- **Email Let's Encrypt** : cedric.tirolf@gmail.com

---

## 2. Application CTSAR-Booking

### Prérequis

1. **DNS configuré** :
   - `ctsar.tirolf.me` doit pointer vers l'IP publique du serveur
   - Vérification : `nslookup ctsar.tirolf.me`

2. **Image Docker disponible** :
   - Repository : `ghcr.io/zaytar68/ctsar-booking:latest`
   - Build automatique via GitHub Actions sur push vers `production`

### Déploiement dans Portainer

1. **Créer un nouveau stack** :
   - Nom : `ctsar-booking`
   - Contenu : copier le fichier [docker-compose.prod.yml](docker-compose.prod.yml)

2. **Configurer les variables d'environnement** :

   Dans la section "Environment variables" de Portainer, ajouter :

   ```
   Application__BaseUrl=https://ctsar.tirolf.me
   SmtpSettings__Host=smtp.free.fr
   SmtpSettings__Port=587
   SmtpSettings__EnableSsl=true
   SmtpSettings__Username=cedric.tirolf@free.fr
   SmtpSettings__Password=VOTRE_MOT_DE_PASSE_SMTP
   SmtpSettings__FromEmail=cedric.tirolf@free.fr
   SmtpSettings__FromName=CTSAR Booking
   ```

   ⚠️ **Important** : Remplacer `VOTRE_MOT_DE_PASSE_SMTP` par le vrai mot de passe

3. **Déployer le stack**

4. **Vérifications** :
   - Conteneur en cours d'exécution : `docker ps | grep ctsar-booking`
   - Logs : `docker logs ctsar-booking`
   - Application accessible : https://ctsar.tirolf.me

---

## 3. Workflow GitHub Actions

### Fonctionnement

1. **Déclenchement** : Push sur la branche `production`
2. **Actions** :
   - Build de l'image Docker
   - Push vers GitHub Container Registry (ghcr.io)
   - Tag : `latest`

3. **Déploiement** :
   - Dans Portainer, cliquer sur "Pull and redeploy" pour le stack `ctsar-booking`
   - Cela téléchargera la nouvelle image et redémarrera l'application

### Fichier de workflow

Voir [.github/workflows/build-production.yml](.github/workflows/build-production.yml)

---

## 4. Maintenance

### Mettre à jour l'application

1. **Développement** :
   ```bash
   git checkout development
   # Faire les modifications
   git add .
   git commit -m "description"
   git push
   ```

2. **Déploiement en production** :
   ```bash
   git checkout production
   git merge development
   git push
   ```

3. **Dans Portainer** :
   - Stack `ctsar-booking` → "Pull and redeploy"

### Consulter les logs

```bash
# Logs Traefik
docker logs traefik -f

# Logs Application
docker logs ctsar-booking -f
```

### Accéder à la base de données

```bash
# Entrer dans le conteneur
docker exec -it ctsar-booking bash

# Base de données SQLite
cd /app/Data
ls -l *.db
```

### Sauvegarder la base de données

```bash
# Créer une sauvegarde
docker cp ctsar-booking:/app/Data/CTSAR.db ./backup-$(date +%Y%m%d).db
```

### Restaurer une sauvegarde

```bash
# Arrêter le conteneur
docker stop ctsar-booking

# Restaurer la base
docker cp ./backup-20250121.db ctsar-booking:/app/Data/CTSAR.db

# Redémarrer le conteneur
docker start ctsar-booking
```

---

## 5. Résolution de problèmes

### L'application ne démarre pas

1. Vérifier les logs :
   ```bash
   docker logs ctsar-booking --tail 100
   ```

2. Vérifier les variables d'environnement dans Portainer

3. Vérifier que le réseau `traefik_default` existe :
   ```bash
   docker network ls | grep traefik
   ```

### Erreur SSL / Certificat Let's Encrypt

1. Vérifier que le DNS est correctement configuré :
   ```bash
   nslookup ctsar.tirolf.me
   ```

2. Vérifier les logs Traefik :
   ```bash
   docker logs traefik | grep -i acme
   docker logs traefik | grep -i letsencrypt
   ```

3. Vérifier que le port 80 est accessible depuis Internet (requis pour le challenge HTTP)

4. Supprimer le certificat et le regénérer :
   ```bash
   docker exec traefik rm /letsencrypt/acme.json
   docker restart traefik
   ```

### Dashboard Traefik inaccessible

1. Vérifier que le port 8080 est bien exposé :
   ```bash
   docker ps | grep traefik
   ```
   Doit afficher : `0.0.0.0:8080->8080/tcp`

2. Tester en local sur le serveur :
   ```bash
   docker exec traefik wget -O- http://localhost:8080/api/rawdata
   ```

3. URL du dashboard : http://192.168.1.253:8080/dashboard/ (ne pas oublier le `/` final)

---

## 6. URLs et Accès

- **Application** : https://ctsar.tirolf.me
- **Dashboard Traefik** : http://192.168.1.253:8080/dashboard/ (réseau local uniquement)
- **Portainer** : Selon votre configuration existante

---

## 7. Sécurité

### Secrets

- ❌ **Jamais dans Git** : Mots de passe, clés API, tokens
- ✅ **Dans Portainer** : Variables d'environnement du stack
- ✅ **Fichier gitignore** : `portainer-env-vars.txt`, `.env`, `appsettings.Production.json`

### Accès

- **Dashboard Traefik** : Réseau local uniquement (192.168.1.0/24)
- **Application** : Public avec HTTPS
- **Portainer** : Configuration existante (à sécuriser avec authentification forte)

### Recommandations

1. Utiliser un mot de passe fort pour le compte administrateur de l'application
2. Activer 2FA si disponible
3. Surveiller les logs régulièrement
4. Maintenir Docker et Traefik à jour

---

## 8. Structure des Fichiers

```
CTSAR-booking/
├── .github/
│   └── workflows/
│       └── build-production.yml      # CI/CD GitHub Actions
├── Components/                        # Code Blazor
├── Data/                             # Contexte EF Core
├── Resources/                        # Fichiers de localisation
├── .dockerignore                     # Fichiers exclus du build Docker
├── Dockerfile                        # Image multi-stage .NET 8
├── docker-compose.prod.yml           # Stack Portainer pour l'application
├── traefik-portainer.yml             # Stack Portainer pour Traefik
├── portainer-env-vars.txt            # Template variables (gitignored)
├── appsettings.json                  # Config dev (sans secrets)
├── appsettings.Production.json       # Config prod (gitignored)
└── DEPLOIEMENT.md                    # Ce fichier
```

---

## Support

Pour toute question ou problème :
1. Consulter les logs Docker
2. Vérifier la configuration DNS
3. Tester en local sur le serveur
4. Consulter la documentation Traefik : https://doc.traefik.io/traefik/
