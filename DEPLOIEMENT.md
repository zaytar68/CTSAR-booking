# Déploiement — CTSAR-Booking

Document de référence pour le déploiement et l'exploitation de l'application de
réservation d'alvéoles.

> Dernière révision : 14 septembre 2026

---

## 1. Architecture

L'application tourne sur un hôte Docker unique, derrière un reverse proxy
Traefik qui gère les certificats Let's Encrypt.

```
Internet
   │
   ▼
Traefik v3.6  ──────── réseau Docker « traefik_default » (externe)
   │                          │
   │  booking.ctsar.eu        │
   ▼                          ▼
ctsar-booking            sqlite-web (optionnel, debug)
  (ASP.NET 8)                 │
   │                          │
   └────── volume « ctsar-data » ──────┘
                 │
                 └── app.db  (SQLite)
```

| Composant | Rôle | Exposition |
|---|---|---|
| `traefik` | Reverse proxy, TLS Let's Encrypt | 80, 443 |
| `portainer` | Administration Docker (Portainer EE 2.45) | 9443 |
| `ctsar-booking` | Application Blazor Web App | via Traefik uniquement |
| `sqlite-web` | Consultation de la base, hors production | `127.0.0.1:8081` |

---

## 2. Image Docker

**Registre** : `ghcr.io/zaytar68/ctsar-booking` — paquet **public**, le `docker pull`
fonctionne sans authentification. Aucun identifiant de registre n'est à
configurer dans Portainer.

**Construction** : GitHub Actions, déclenché par un push sur la branche
`production`. Le workflow construit l'image (Dockerfile multi-étapes .NET 8) et
pousse deux tags :

- `latest` — toujours la dernière version de `production`
- `production-<sha-court>` — version figée, utilisable pour un retour arrière

Vérifier les tags disponibles :

```bash
curl -s "https://ghcr.io/token?scope=repository:zaytar68/ctsar-booking:pull&service=ghcr.io" \
  | jq -r .token \
  | xargs -I{} curl -s -H "Authorization: Bearer {}" \
      https://ghcr.io/v2/zaytar68/ctsar-booking/tags/list | jq .
```

---

## 3. Persistance des données

La base SQLite est **`app.db`**, dans le volume Docker `ctsar-data`, monté sur
`/app/Data` dans le conteneur applicatif.

Le volume est déclaré **`external`**. C'est délibéré : un volume nommé
ordinaire est préfixé par le nom du projet Compose (donc par le nom du stack
Portainer), et se retrouve donc recréé vide au moindre renommage du stack.
En externe, il est indépendant du cycle de vie des stacks.

Si le volume n'existe pas encore sur un nouvel hôte :

```bash
docker volume create ctsar-data
```

---

## 4. Déploiement

Le stack est déployé depuis Portainer : **Stacks → Add stack → Web editor**,
nommé `ctsar-booking`.

### 4.1 docker-compose

```yaml
services:
  ctsar-booking:
    image: ghcr.io/zaytar68/ctsar-booking:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - Application__BaseUrl=${Application__BaseUrl}
      - SmtpSettings__Host=${SmtpSettings__Host}
      - SmtpSettings__Port=${SmtpSettings__Port}
      - SmtpSettings__EnableSsl=${SmtpSettings__EnableSsl}
      - SmtpSettings__Username=${SmtpSettings__Username}
      - SmtpSettings__Password=${SmtpSettings__Password}
      - SmtpSettings__FromEmail=${SmtpSettings__FromEmail}
      - SmtpSettings__FromName=${SmtpSettings__FromName}
      - VapidSettings__Subject=${VapidSettings__Subject}
      - VapidSettings__PublicKey=${VapidSettings__PublicKey}
      - VapidSettings__PrivateKey=${VapidSettings__PrivateKey}
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.ctsar.rule=Host(`booking.ctsar.eu`)"
      - "traefik.http.routers.ctsar.entrypoints=websecure"
      - "traefik.http.routers.ctsar.tls.certresolver=letsencrypt"
      - "traefik.http.services.ctsar.loadbalancer.server.port=8080"
    volumes:
      - ctsar-data:/app/Data
    networks:
      - traefik_default
    restart: unless-stopped

  sqlite-web:
    image: coleifer/sqlite-web:latest
    environment:
      - SQLITE_DATABASE=/data/app.db
    labels:
      - "traefik.enable=false"
    ports:
      - "127.0.0.1:8081:8080"
    volumes:
      - ctsar-data:/data
    networks:
      - traefik_default
    restart: unless-stopped

networks:
  traefik_default:
    external: true

volumes:
  ctsar-data:
    external: true
    name: ctsar-data
```

Deux choix à noter :

- **Pas de `container_name`.** Compose nomme lui-même les conteneurs
  `<stack>-<service>-1`. Fixer les noms empêche deux stacks de coexister et
  oblige à renommer lors d'une recréation, ce qui est la source d'erreur
  décrite au §8.1.
- **`sqlite-web` ne porte aucun label Traefik.** Il n'est joignable que depuis
  l'hôte (§7).

### 4.2 Variables d'environnement

À saisir dans la section *Environment variables* du stack Portainer. Elles ne
sont **jamais** versionnées dans le dépôt.

| Variable | Exemple / valeur |
|---|---|
| `Application__BaseUrl` | `https://booking.ctsar.eu` |
| `SmtpSettings__Host` | `smtp.free.fr` |
| `SmtpSettings__Port` | `587` |
| `SmtpSettings__EnableSsl` | `true` |
| `SmtpSettings__Username` | compte SMTP |
| `SmtpSettings__Password` | **secret** |
| `SmtpSettings__FromEmail` | adresse d'expédition |
| `SmtpSettings__FromName` | nom affiché |
| `VapidSettings__Subject` | `mailto:…` (notifications push) |
| `VapidSettings__PublicKey` | clé publique VAPID |
| `VapidSettings__PrivateKey` | **secret** |

### 4.3 Prérequis côté hôte

- Le réseau `traefik_default` doit exister (il est créé par le stack Traefik).
  Vérifier : `docker network ls | grep traefik`
- Le volume `ctsar-data` doit exister (§3).
- Le DNS `booking.ctsar.eu` doit pointer sur l'IP publique, et les ports 80/443
  être redirigés vers l'hôte — le challenge HTTP de Let's Encrypt en dépend.

---

## 5. Mise à jour

### 5.1 Procédure manuelle

1. Fusionner les changements dans la branche `production`.
2. Attendre la fin du workflow GitHub Actions (nouvelle image poussée).
3. Dans Portainer : stack `ctsar-booking` → **Pull and redeploy**.

### 5.2 Automatisation par webhook

Portainer Business Edition permet de déclencher ce redéploiement par appel HTTP.

**Activation** : stack `ctsar-booking` → onglet *Editor* → section *Webhooks* →
**Create a stack webhook** → copier l'URL.

```
https://<portainer>/api/stacks/webhooks/<uuid>
```

L'UUID tient lieu de secret : aucune authentification n'est demandée. À traiter
comme un mot de passe.

Un `POST` sur cette URL re-pull les images et recrée les conteneurs. Les volumes
sont conservés. **L'appel retourne avant la fin du déploiement** : un code 2xx
signifie « demande acceptée », pas « déploiement réussi ».

Paramètres disponibles :

| Paramètre | Effet |
|---|---|
| `?pullimage=false` | redéployer sans re-télécharger les images |
| `?<VAR>=valeur` | injecter une variable d'environnement dans le compose |

**Joignabilité** : Portainer n'est pas exposé sur Internet. Un runner GitHub
hébergé ne peut donc pas appeler ce webhook directement. Deux approches :

- *Recommandé* — une tâche planifiée sur l'hôte qui compare le digest distant au
  digest local et appelle le webhook en local, sans rien exposer :

  ```bash
  #!/bin/bash
  REMOTE=$(docker manifest inspect ghcr.io/zaytar68/ctsar-booking:latest | sha256sum | cut -d' ' -f1)
  LOCAL=$(cat /var/lib/ctsar-digest 2>/dev/null)
  if [ "$REMOTE" != "$LOCAL" ]; then
    curl -sk -X POST "https://127.0.0.1:9443/api/stacks/webhooks/<uuid>"
    echo "$REMOTE" > /var/lib/ctsar-digest
  fi
  ```

- Exposer *uniquement* le chemin `/api/stacks/webhooks/` via Traefik, sur un
  hôte dédié et derrière un middleware `basicauth`. Ne jamais publier
  l'interface Portainer complète.

### 5.3 Retour arrière

Remplacer le tag dans le compose par une version figée, puis redéployer :

```yaml
image: ghcr.io/zaytar68/ctsar-booking:production-256aec7
```

---

## 6. Sauvegarde et restauration

### 6.1 Sauvegarde

Archive complète du volume, conteneur arrêté :

```bash
docker compose -p ctsar-booking stop
docker run --rm -v ctsar-data:/d -v "$HOME/backup":/b alpine \
  tar czf /b/ctsar-$(date +%F).tgz -C /d .
docker compose -p ctsar-booking start
```

À chaud, sans interruption de service, via l'API de sauvegarde SQLite — qui
gère correctement le journal WAL, contrairement à une simple copie de fichier :

```bash
docker run --rm -v ctsar-data:/d -v "$HOME/backup":/b keinos/sqlite3 \
  sqlite3 /d/app.db ".backup '/b/app-$(date +%F).db'"
```

À planifier quotidiennement (cron ou timer systemd), avec une rétention d'au
moins 30 jours. C'est la seule protection contre une migration EF Core qui se
passerait mal lors d'un déploiement automatique.

### 6.2 Restauration

```bash
docker compose -p ctsar-booking stop
docker run --rm -v ctsar-data:/d -v "$HOME/backup":/b alpine \
  sh -c 'rm -f /d/app.db*; cp /b/app-2026-09-14.db /d/app.db'
docker compose -p ctsar-booking start
```

Les fichiers `app.db-wal` et `app.db-shm` doivent être supprimés en même temps
que la base : restaurer un `.db` en laissant l'ancien WAL à côté corrompt
l'ensemble.

---

## 7. Accès à la base

`sqlite-web` n'écoute que sur la loopback de l'hôte. Il n'a **aucune
authentification** et donne un accès en écriture complet : il ne doit jamais
être exposé au réseau local ni à Internet.

Depuis un poste distant, passer par un tunnel SSH :

```bash
ssh -L 8081:127.0.0.1:8081 <utilisateur>@<hôte>
# puis ouvrir http://127.0.0.1:8081
```

Deux conteneurs écrivant simultanément la même base SQLite est une
configuration fragile. Pour ne démarrer `sqlite-web` qu'à la demande, lui
ajouter :

```yaml
    profiles: ["debug"]
```

---

## 8. Pièges connus

### 8.1 Le volume recréé vide au renommage du stack

**Symptôme** : après recréation du stack sous un nom différent, l'application
démarre sur une base vide.

**Cause** : un volume déclaré `driver: local` est nommé
`<nom-du-stack>_<nom-du-volume>`. Changer le nom du stack crée un nouveau
volume, et EF Core y initialise une base neuve au démarrage. Les données ne sont
pas perdues — elles sont dans l'ancien volume.

**Diagnostic** :

```bash
docker volume ls | grep -i ctsar
docker inspect <ancien-conteneur> \
  --format '{{range .Mounts}}{{.Name}} => {{.Destination}}{{end}}'
```

**Correctif** : le volume `external` du §3. C'est la raison de ce choix.

### 8.2 Collision de noms de routeurs Traefik

Deux services déclarant `traefik.http.routers.**ctsar**.rule` — même inertes
via `traefik.enable=false` — entrent en conflit dès que l'un est activé. Le nom
du routeur doit être unique dans toute l'instance Traefik, pas seulement dans le
stack.

### 8.3 Stack créé hors Portainer

Un stack déployé en ligne de commande apparaît dans Portainer en lecture seule :
pas d'éditeur, pas de webhook. Pour en reprendre la main, il faut le supprimer
(`docker compose down`, qui conserve les volumes nommés) puis le recréer depuis
l'interface. Avec un volume `external`, l'opération est sans risque pour les
données.

### 8.4 Nom du fichier de base

La base s'appelle **`app.db`**, et non `CTSAR.db`. Toute configuration pointant
sur `CTSAR.db` ouvrira un fichier inexistant — SQLite en crée alors un vide,
sans erreur, ce qui donne l'illusion d'une perte de données.

---

## 9. Vérifications après déploiement

```bash
# conteneurs en marche
docker ps --filter name=ctsar

# la base est bien montée et non vide
docker run --rm -v ctsar-data:/d alpine ls -la /d

# le site répond
curl -sI https://booking.ctsar.eu | head -1

# certificat valide
echo | openssl s_client -connect booking.ctsar.eu:443 2>/dev/null \
  | openssl x509 -noout -dates
```

Et, dans l'interface : se connecter, vérifier que les réservations existantes
sont présentes, et créer puis annuler une réservation de test pour valider les
écritures.
