# Déploiement — CTSAR-Booking

Document de référence pour le déploiement et l'exploitation de l'application de
réservation d'alvéoles.

> Dernière révision : 29 octobre 2026

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
ctsar-booking            sqlite-web (optionnel, debug)        backup
  (ASP.NET 8)                 │                                  │
   │                          │                                  │
   └──── /home/cedric/appdata/CTSAR-booking/data ────────────────┤
                 ├── app.db  (SQLite)                            │
                 └── keys/   (clés Data Protection)              │
                                                                 ▼
                      /home/cedric/appdata/CTSAR-booking/backups
```

| Composant | Rôle | Exposition |
|---|---|---|
| `traefik` | Reverse proxy, TLS Let's Encrypt | 80, 443 |
| `portainer` | Administration Docker (Portainer EE 2.45) | 9443 |
| `ctsar-booking` | Application Blazor Web App | via Traefik uniquement |
| `sqlite-web` | Consultation de la base, hors production | `127.0.0.1:8081` |
| `backup` | Sauvegarde quotidienne de la base (§6) | aucune |

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

Les données sont stockées dans des dossiers de l'hôte, montés en **bind mount** :

| Dossier hôte (par défaut) | Variable Portainer | Monté sur | Contenu |
|---|---|---|---|
| `/home/cedric/appdata/CTSAR-booking/data` | `CTSAR_DATA_PATH` | `/app/Data` | `app.db` (+ `-wal`, `-shm`), `keys/` |
| `/home/cedric/appdata/CTSAR-booking/backups` | `CTSAR_BACKUP_PATH` | `/backups` (service `backup`) | `app-AAAAMMJJ-HHMMSS.db.gz` |

Le bind mount est délibéré : comme un volume `external`, il est indépendant du
cycle de vie des stacks (pas de recréation à vide au renommage, §8.1), et les
fichiers sont directement accessibles sur l'hôte pour les sauvegardes et les
copies hors site.

`keys/` contient les clés Data Protection qui chiffrent les cookies
d'authentification. Elles sont persistées pour que les utilisateurs restent
connectés après un redéploiement. Les perdre n'entraîne qu'une déconnexion.

Sur un nouvel hôte :

```bash
mkdir -p /home/cedric/appdata/CTSAR-booking/data /home/cedric/appdata/CTSAR-booking/backups
```

---

## 4. Déploiement

Le stack est déployé depuis Portainer : **Stacks → Add stack → Web editor**,
nommé `ctsar-booking`.

### 4.1 docker-compose

Le contenu à coller dans l'éditeur du stack est le fichier
[docker-compose.prod.yml](docker-compose.prod.yml) du dépôt : c'est la
référence, ne pas le recopier ici pour éviter les divergences.

Il définit trois services : `ctsar-booking`, `sqlite-web` et `backup`.

Choix à noter :

- **Pas de `container_name`.** Compose nomme lui-même les conteneurs
  `<stack>-<service>-1`. Fixer les noms empêche deux stacks de coexister et
  oblige à renommer lors d'une recréation, ce qui est la source d'erreur
  décrite au §8.1.
- **`sqlite-web` ne porte aucun label Traefik.** Il n'est joignable que depuis
  l'hôte (§7).
- **Bind mounts plutôt que volume Docker** (§3).

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

Facultatives (valeurs par défaut dans le compose) :

| Variable | Défaut |
|---|---|
| `CTSAR_DATA_PATH` | `/home/cedric/appdata/CTSAR-booking/data` |
| `CTSAR_BACKUP_PATH` | `/home/cedric/appdata/CTSAR-booking/backups` |
| `BACKUP_TIME` | `03:00` (heure de Paris) |
| `BACKUP_RETENTION_DAYS` | `30` |

### 4.3 Prérequis côté hôte

- Le réseau `traefik_default` doit exister (il est créé par le stack Traefik).
  Vérifier : `docker network ls | grep traefik`
- Les dossiers `data` et `backups` doivent exister (§3).
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

### 6.1 Sauvegarde automatique

Le service `backup` du stack effectue chaque jour à `BACKUP_TIME` une
sauvegarde **à chaud**, sans interruption de service, via l'API de sauvegarde
SQLite — qui gère correctement le journal WAL, contrairement à une simple copie
de fichier. Chaque sauvegarde est vérifiée (`PRAGMA integrity_check`),
compressée, puis les fichiers plus anciens que `BACKUP_RETENTION_DAYS` sont
supprimés.

C'est la seule protection contre une migration EF Core qui se passerait mal
lors d'un déploiement automatique.

```bash
# Sauvegarde manuelle immédiate (par exemple avant un déploiement)
docker exec ctsar-booking-backup-1 backup-now

# Lister les sauvegardes
ls -lh /home/cedric/appdata/CTSAR-booking/backups

# Journal du service
docker logs ctsar-booking-backup-1
```

⚠️ Les sauvegardes restent sur le même hôte. Copier régulièrement le dossier
`backups` hors site (NAS, rclone, rsync…).

### 6.2 Restauration

```bash
docker compose -p ctsar-booking stop
cd /home/cedric/appdata/CTSAR-booking/data
rm -f app.db app.db-wal app.db-shm
gunzip -c ../backups/app-20260914-030000.db.gz > app.db
docker compose -p ctsar-booking start
```

Les fichiers `app.db-wal` et `app.db-shm` doivent être supprimés en même temps
que la base : restaurer un `.db` en laissant l'ancien WAL à côté corrompt
l'ensemble.

### 6.3 Migration depuis le volume `ctsar-data`

À faire **une seule fois**, avant de déployer le compose à bind mounts — sinon
l'application démarre sur un dossier vide et EF Core y crée une base neuve.

```bash
# 1. Arrêter le stack (Portainer → Stacks → ctsar-booking → Stop)

# 2. Créer les dossiers
mkdir -p /home/cedric/appdata/CTSAR-booking/data /home/cedric/appdata/CTSAR-booking/backups

# 3. Copier le contenu du volume
docker run --rm \
  -v ctsar-data:/from:ro \
  -v /home/cedric/appdata/CTSAR-booking/data:/to \
  alpine sh -c "cp -a /from/. /to/ && ls -l /to"
```

`app.db` doit apparaître dans la liste. Mettre ensuite à jour le stack avec le
nouveau compose et déployer. Le volume `ctsar-data` reste intact et sert de
retour arrière : ne le supprimer (`docker volume rm ctsar-data`) qu'après
quelques jours sans problème.

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

**Correctif** : les bind mounts du §3, indépendants du nom du stack.

### 8.2 Collision de noms de routeurs Traefik

Deux services déclarant `traefik.http.routers.**ctsar**.rule` — même inertes
via `traefik.enable=false` — entrent en conflit dès que l'un est activé. Le nom
du routeur doit être unique dans toute l'instance Traefik, pas seulement dans le
stack.

### 8.3 Stack créé hors Portainer

Un stack déployé en ligne de commande apparaît dans Portainer en lecture seule :
pas d'éditeur, pas de webhook. Pour en reprendre la main, il faut le supprimer
(`docker compose down`, qui conserve les volumes nommés) puis le recréer depuis
l'interface. Avec les bind mounts, l'opération est sans risque pour les
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
ls -la /home/cedric/appdata/CTSAR-booking/data

# le service de sauvegarde tourne
docker logs ctsar-booking-backup-1

# le site répond
curl -sI https://booking.ctsar.eu | head -1

# certificat valide
echo | openssl s_client -connect booking.ctsar.eu:443 2>/dev/null \
  | openssl x509 -noout -dates
```

Et, dans l'interface : se connecter, vérifier que les réservations existantes
sont présentes, et créer puis annuler une réservation de test pour valider les
écritures.
