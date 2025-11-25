// ====================================================================
// API PUSH NOTIFICATIONS - CTSAR Booking
// Gère l'enregistrement et la gestion des notifications push côté client
// ====================================================================

window.pushNotifications = {
    vapidPublicKey: null,
    serviceWorkerRegistration: null,

    // ================================================================
    // INITIALISATION
    // ================================================================

    /**
     * Initialise le système de push notifications avec la clé publique VAPID
     * @param {string} publicKey - Clé publique VAPID en Base64
     */
    initialize: function(publicKey) {
        this.vapidPublicKey = publicKey;
        console.log('[Push Notifications] Initialized with VAPID public key');

        // Enregistrer le service worker
        this.registerServiceWorker();
    },

    /**
     * Enregistre le service worker
     */
    registerServiceWorker: async function() {
        if (!('serviceWorker' in navigator)) {
            console.warn('[Push Notifications] Service Workers not supported');
            return false;
        }

        try {
            const registration = await navigator.serviceWorker.register('/service-worker.js', {
                scope: '/'
            });

            this.serviceWorkerRegistration = registration;
            console.log('[Push Notifications] Service Worker registered:', registration.scope);

            // Attendre que le service worker soit prêt
            await navigator.serviceWorker.ready;
            console.log('[Push Notifications] Service Worker ready');

            return true;
        } catch (error) {
            console.error('[Push Notifications] Service Worker registration failed:', error);
            return false;
        }
    },

    // ================================================================
    // VÉRIFICATIONS DE SUPPORT
    // ================================================================

    /**
     * Vérifie si les notifications push sont supportées par le navigateur
     * @returns {boolean}
     */
    isSupported: function() {
        return 'serviceWorker' in navigator &&
               'PushManager' in window &&
               'Notification' in window;
    },

    /**
     * Récupère le statut actuel de la permission de notification
     * @returns {string} 'default', 'granted', 'denied', ou 'unsupported'
     */
    getPermissionStatus: function() {
        if (!this.isSupported()) {
            return 'unsupported';
        }

        return Notification.permission; // 'default', 'granted', 'denied'
    },

    // ================================================================
    // GESTION DES PERMISSIONS
    // ================================================================

    /**
     * Demande la permission d'afficher des notifications
     * @returns {Promise<string>} Permission accordée : 'granted', 'denied', 'default'
     */
    requestPermission: async function() {
        if (!this.isSupported()) {
            throw new Error('Push notifications non supportées par ce navigateur');
        }

        try {
            const permission = await Notification.requestPermission();
            console.log('[Push Notifications] Permission:', permission);
            return permission;
        } catch (error) {
            console.error('[Push Notifications] Permission request failed:', error);
            throw error;
        }
    },

    // ================================================================
    // SOUSCRIPTION AUX NOTIFICATIONS
    // ================================================================

    /**
     * Souscrit aux notifications push
     * @returns {Promise<Object>} Objet de souscription { endpoint, p256dh, auth }
     */
    subscribe: async function() {
        if (!this.isSupported()) {
            throw new Error('Push notifications non supportées');
        }

        if (Notification.permission !== 'granted') {
            throw new Error('Permission de notification non accordée');
        }

        if (!this.vapidPublicKey) {
            throw new Error('Clé publique VAPID non initialisée');
        }

        try {
            // Attendre que le service worker soit prêt
            const registration = await navigator.serviceWorker.ready;

            // Vérifier si déjà abonné
            let subscription = await registration.pushManager.getSubscription();

            if (subscription) {
                console.log('[Push Notifications] Already subscribed');
            } else {
                // Créer une nouvelle souscription
                const convertedKey = this.urlBase64ToUint8Array(this.vapidPublicKey);

                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: convertedKey
                });

                console.log('[Push Notifications] New subscription created');
            }

            // Convertir la souscription en format JSON pour l'envoyer au serveur
            const subscriptionJson = {
                endpoint: subscription.endpoint,
                p256dh: this.arrayBufferToBase64(subscription.getKey('p256dh')),
                auth: this.arrayBufferToBase64(subscription.getKey('auth'))
            };

            console.log('[Push Notifications] Subscription:', subscriptionJson);
            return subscriptionJson;

        } catch (error) {
            console.error('[Push Notifications] Subscription failed:', error);
            throw error;
        }
    },

    /**
     * Se désabonne des notifications push
     * @returns {Promise<boolean>}
     */
    unsubscribe: async function() {
        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();

            if (subscription) {
                const success = await subscription.unsubscribe();
                console.log('[Push Notifications] Unsubscribed:', success);
                return success;
            }

            console.log('[Push Notifications] No active subscription');
            return false;

        } catch (error) {
            console.error('[Push Notifications] Unsubscribe failed:', error);
            throw error;
        }
    },

    /**
     * Vérifie si l'utilisateur est actuellement abonné
     * @returns {Promise<boolean>}
     */
    isSubscribed: async function() {
        try {
            if (!this.isSupported()) {
                return false;
            }

            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();

            return subscription !== null;
        } catch (error) {
            console.error('[Push Notifications] Failed to check subscription:', error);
            return false;
        }
    },

    // ================================================================
    // UTILITAIRES DE CONVERSION
    // ================================================================

    /**
     * Convertit une clé VAPID Base64 URL-safe en Uint8Array
     * @param {string} base64String - Clé en Base64 URL-safe
     * @returns {Uint8Array}
     */
    urlBase64ToUint8Array: function(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }

        return outputArray;
    },

    /**
     * Convertit un ArrayBuffer en chaîne Base64
     * @param {ArrayBuffer} buffer
     * @returns {string}
     */
    arrayBufferToBase64: function(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';

        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }

        return window.btoa(binary);
    },

    // ================================================================
    // NOTIFICATION DE TEST
    // ================================================================

    /**
     * Affiche une notification de test locale (ne passe pas par le serveur)
     * @param {string} title - Titre de la notification
     * @param {string} body - Corps de la notification
     */
    showTestNotification: async function(title = 'Test', body = 'Notification de test') {
        if (!this.isSupported()) {
            throw new Error('Notifications non supportées');
        }

        if (Notification.permission !== 'granted') {
            throw new Error('Permission non accordée');
        }

        try {
            const registration = await navigator.serviceWorker.ready;

            await registration.showNotification(title, {
                body: body,
                icon: '/icons/icon-192x192.png',
                badge: '/icons/icon-192x192.png',
                vibrate: [200, 100, 200],
                data: { url: '/' }
            });

            console.log('[Push Notifications] Test notification displayed');
            return true;
        } catch (error) {
            console.error('[Push Notifications] Test notification failed:', error);
            throw error;
        }
    },

    // ================================================================
    // COMMUNICATION AVEC LE SERVEUR
    // ================================================================

    /**
     * Envoie la souscription au serveur backend
     * @returns {Promise<boolean>}
     */
    sendSubscriptionToServer: async function() {
        try {
            const subscription = await this.subscribe();

            const response = await fetch('/api/pushnotifications/subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(subscription)
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            console.log('[Push Notifications] Subscription sent to server');
            return true;

        } catch (error) {
            console.error('[Push Notifications] Failed to send subscription to server:', error);
            throw error;
        }
    },

    /**
     * Supprime la souscription du serveur
     * @returns {Promise<boolean>}
     */
    removeSubscriptionFromServer: async function() {
        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();

            if (!subscription) {
                return false;
            }

            const response = await fetch('/api/pushnotifications/unsubscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    endpoint: subscription.endpoint
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            // Désabonner localement
            await this.unsubscribe();

            console.log('[Push Notifications] Subscription removed from server');
            return true;

        } catch (error) {
            console.error('[Push Notifications] Failed to remove subscription from server:', error);
            throw error;
        }
    }
};

// ====================================================================
// ENREGISTREMENT AUTOMATIQUE DU SERVICE WORKER AU CHARGEMENT
// ====================================================================
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/service-worker.js')
            .then(registration => {
                console.log('[Service Worker] Registered on page load:', registration.scope);
            })
            .catch(error => {
                console.error('[Service Worker] Registration failed on page load:', error);
            });
    });
}

console.log('[Push Notifications] API loaded');
