if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('js/service-worker.js')
        .then(function (registration) {
            console.log('Service Worker registered with scope:', registration.scope);
        }).catch(function (error) {
        console.log('Service Worker registration failed:', error);
    });
}
window.blazorCulture = {
    get: () => window.localStorage['BlazorCulture'], set: (value) => window.localStorage['BlazorCulture'] = value
};

function deleteTemplateCache() {
    caches.keys().then(function (names) {
        for (let name of names) caches.delete(name);
    });
}


window.requestNotificationPermission = async () => {
    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
        console.log('Notification permission not granted.');
    }
}
window.subscribeUserToPush = async () => {
    const register = await navigator.serviceWorker.ready;
    const subscription = await register.pushManager.subscribe({
        userVisibleOnly: true, applicationServerKey: 'BCpg-oRcOjQLQZs5N_7MBdIqaDDui3G4C_0F3y6iazjmjCyrzYag_Bkh-1KFPLq7eLQggBWkskaYrY_7XWiABYM'
    });

    return {
        endpoint: subscription.endpoint, p256dh: btoa(String.fromCharCode.apply(null, new Uint8Array(subscription.getKey('p256dh')))), auth: btoa(String.fromCharCode.apply(null, new Uint8Array(subscription.getKey('auth'))))
    };
};

