if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/js/service-worker.js')
        .then(function (registration) {
            console.log('Service Worker registered with scope:', registration.scope);
        }).catch(function (error) {
        console.log('Service Worker registration failed:', error);
    });
}

window.deleteTemplateCache = () => {
    caches.keys().then(function (names) {
        for (let name of names) caches.delete(name);
    });
}

window.CloseProgressBar = () => {
    const progressWrapper = document.getElementById("progress-wrapper");
    progressWrapper.classList.add('closed');
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

window.getCookie = (cname) => {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

window.setCookie = (cname, cvalue, exdays) => {
    const d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
}

function PageShowEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'PageShowEventEventListener')
}

function PageHideEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'PageHideEventEventListener')
}

function ContextMenuEvent(e) {
    e.preventDefault();
    DotNet.invokeMethodAsync("WebApp.Client", 'ContextMenuEventListener')
    DotNet.invokeMethodAsync("WebApp.Client", 'ContextMenuEventListenerWithParam', e.clientX, e.clientY)
}

function EnterClickEvent(event) {
    if (event.key === "Enter") {
        DotNet.invokeMethodAsync("WebApp.Client", 'EnterEventListener')
    }
}

function OfflineEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'OfflineEventListener')

}

function OnlineEvent() {
    DotNet.invokeMethodAsync("WebApp.Client", 'OnlineEventListener')
}


//
// Events Listener
//

window.InitAppEventListener = () => {
    window.addEventListener("pagehide", PageHideEvent);
    window.addEventListener("pageshow", PageShowEvent);
    window.addEventListener("contextmenu", ContextMenuEvent);
    window.addEventListener("keydown", EnterClickEvent)
    window.addEventListener('online', OnlineEvent);
    window.addEventListener('offline', OfflineEvent);
}

