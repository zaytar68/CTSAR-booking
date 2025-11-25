// ====================================================================
// SERVICE WORKER - CTSAR Booking
// Gère le cache offline et les notifications push
// ====================================================================

// Version du cache - Incrémenter à chaque changement pour forcer la mise à jour
const CACHE_VERSION = 'v1.0.0';
const CACHE_NAME = `ctsar-booking-${CACHE_VERSION}`;

// Fichiers à mettre en cache pour le mode offline
const STATIC_ASSETS = [
    '/',
    '/app.css',
    '/icons/icon-192x192.png',
    '/icons/icon-512x512.png',
    '/manifest.json'
];

// ====================================================================
// INSTALLATION DU SERVICE WORKER
// ====================================================================
self.addEventListener('install', event => {
    console.log('[Service Worker] Installing...');

    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[Service Worker] Caching static assets');
                return cache.addAll(STATIC_ASSETS);
            })
            .then(() => {
                console.log('[Service Worker] Installation complete');
                // Force l'activation immédiate du nouveau service worker
                return self.skipWaiting();
            })
            .catch(error => {
                console.error('[Service Worker] Installation failed:', error);
            })
    );
});

// ====================================================================
// ACTIVATION DU SERVICE WORKER
// ====================================================================
self.addEventListener('activate', event => {
    console.log('[Service Worker] Activating...');

    event.waitUntil(
        caches.keys()
            .then(cacheNames => {
                // Supprimer les anciens caches
                return Promise.all(
                    cacheNames
                        .filter(name => name !== CACHE_NAME)
                        .map(name => {
                            console.log('[Service Worker] Deleting old cache:', name);
                            return caches.delete(name);
                        })
                );
            })
            .then(() => {
                console.log('[Service Worker] Activation complete');
                // Prendre le contrôle immédiatement de toutes les pages
                return self.clients.claim();
            })
    );
});

// ====================================================================
// GESTION DES REQUÊTES RÉSEAU (stratégie cache-first pour offline)
// ====================================================================
self.addEventListener('fetch', event => {
    // Ignorer les requêtes non-GET (POST, PUT, DELETE, etc.)
    if (event.request.method !== 'GET') {
        return;
    }

    // Ignorer les requêtes vers les APIs externes
    if (!event.request.url.startsWith(self.location.origin)) {
        return;
    }

    event.respondWith(
        caches.match(event.request)
            .then(cachedResponse => {
                if (cachedResponse) {
                    // Fichier en cache : le retourner immédiatement
                    return cachedResponse;
                }

                // Pas en cache : récupérer depuis le réseau
                return fetch(event.request)
                    .then(response => {
                        // Ne pas mettre en cache les réponses non-200
                        if (!response || response.status !== 200 || response.type === 'error') {
                            return response;
                        }

                        // Cloner la réponse pour pouvoir la mettre en cache
                        const responseToCache = response.clone();

                        caches.open(CACHE_NAME)
                            .then(cache => {
                                cache.put(event.request, responseToCache);
                            });

                        return response;
                    })
                    .catch(error => {
                        console.error('[Service Worker] Fetch failed:', error);
                        // Retourner une page d'erreur offline si disponible
                        return caches.match('/offline.html');
                    });
            })
    );
});

// ====================================================================
// RÉCEPTION DE NOTIFICATIONS PUSH
// ====================================================================
self.addEventListener('push', event => {
    console.log('[Service Worker] Push notification received:', event);

    // Extraire les données de la notification
    const data = event.data ? event.data.json() : {};

    const title = data.title || 'CTSAR Booking';
    const options = {
        body: data.body || 'Nouvelle notification',
        icon: data.icon || '/icons/icon-192x192.png',
        badge: data.badge || '/icons/icon-192x192.png',
        data: {
            url: data.url || '/',
            timestamp: Date.now()
        },
        vibrate: [200, 100, 200], // Pattern de vibration
        tag: data.tag || 'default', // Tag pour grouper les notifications
        requireInteraction: data.requireInteraction || false,
        actions: data.actions || [] // Boutons d'action (si supportés)
    };

    // Afficher la notification
    event.waitUntil(
        self.registration.showNotification(title, options)
            .then(() => {
                console.log('[Service Worker] Notification displayed');
            })
            .catch(error => {
                console.error('[Service Worker] Failed to show notification:', error);
            })
    );
});

// ====================================================================
// CLIC SUR UNE NOTIFICATION
// ====================================================================
self.addEventListener('notificationclick', event => {
    console.log('[Service Worker] Notification clicked:', event.notification.tag);

    // Fermer la notification
    event.notification.close();

    // Gérer les actions spécifiques (boutons)
    if (event.action) {
        console.log('[Service Worker] Action clicked:', event.action);
        // Gérer les actions personnalisées ici
    }

    // Récupérer l'URL de destination
    const urlToOpen = event.notification.data.url || '/';

    // Ouvrir ou focus la fenêtre de l'application
    event.waitUntil(
        clients.matchAll({
            type: 'window',
            includeUnstarted: true
        })
        .then(clientList => {
            // Chercher une fenêtre déjà ouverte avec la même URL
            for (let client of clientList) {
                if (client.url === urlToOpen && 'focus' in client) {
                    console.log('[Service Worker] Focusing existing window');
                    return client.focus();
                }
            }

            // Aucune fenêtre trouvée : en ouvrir une nouvelle
            if (clients.openWindow) {
                console.log('[Service Worker] Opening new window:', urlToOpen);
                return clients.openWindow(urlToOpen);
            }
        })
        .catch(error => {
            console.error('[Service Worker] Failed to handle notification click:', error);
        })
    );
});

// ====================================================================
// FERMETURE D'UNE NOTIFICATION (sans clic)
// ====================================================================
self.addEventListener('notificationclose', event => {
    console.log('[Service Worker] Notification closed:', event.notification.tag);

    // Analytics ou logging si nécessaire
    // Exemple : envoyer une stat au serveur pour mesurer le taux de clic
});

// ====================================================================
// GESTION DES ERREURS
// ====================================================================
self.addEventListener('error', event => {
    console.error('[Service Worker] Error:', event.error);
});

self.addEventListener('unhandledrejection', event => {
    console.error('[Service Worker] Unhandled Promise rejection:', event.reason);
});

// ====================================================================
// MESSAGE DU CLIENT VERS LE SERVICE WORKER
// ====================================================================
self.addEventListener('message', event => {
    console.log('[Service Worker] Message received:', event.data);

    if (event.data && event.data.type === 'SKIP_WAITING') {
        // Forcer l'activation immédiate du nouveau service worker
        self.skipWaiting();
    }

    if (event.data && event.data.type === 'CLEAR_CACHE') {
        // Vider le cache
        event.waitUntil(
            caches.keys()
                .then(cacheNames => {
                    return Promise.all(
                        cacheNames.map(name => caches.delete(name))
                    );
                })
                .then(() => {
                    console.log('[Service Worker] Cache cleared');
                    event.ports[0].postMessage({ success: true });
                })
        );
    }
});

console.log('[Service Worker] Script loaded');
