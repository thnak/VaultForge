if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/js/service-worker.js')
        .then(function (registration) {
            console.log('Service Worker registered with scope:', registration.scope);
        }).catch(function (error) {
        console.log('Service Worker registration failed:', error);
    });
}

window.CloseProgressBar = () => {
    const progressWrapper = document.getElementById("progress-wrapper");
    if(progressWrapper)
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
}

window.getCultureFromCookie = (cookieName = 'thnakdevserverCulture') => {
    // Get the cookie string
    const cookieString = document.cookie;

    // Find the culture cookie by name
    const cultureCookie = cookieString.split('; ').find(row => row.startsWith(cookieName + '='));

    if (cultureCookie) {
        // Decode the cookie value (URL decoding)
        const decodedValue = decodeURIComponent(cultureCookie.split('=')[1]);

        // Split the value to get both culture (c) and UI culture (uic)
        const cultureInfo = decodedValue.split('%7C'); // %7C is the encoded form of "|"

        // Extract the 'c=' part (culture)
        const culture = cultureInfo[0].split('=')[1]; // 'c=vi-VN' -> 'vi-VN'

        return culture.split('|')[0];
    }

    // Return null if no culture cookie is found
    return null;
}

window.setCultureCookie = (culture, uiCulture, cookieName = 'thnakdevserverCulture', daysToExpire = 7) => {
    // Encode the cookie value (culture and uiCulture)
    const cookieValue = encodeURIComponent(`c=${culture}|uic=${uiCulture}`);

    // Calculate the expiration date
    const date = new Date();
    date.setTime(date.getTime() + (daysToExpire * 24 * 60 * 60 * 1000)); // Convert days to milliseconds

    // Create the cookie with the name, value, and expiration date
    document.cookie = `${cookieName}=${cookieValue};expires=${date.toUTCString()};path=/`;
    const htmlElement = document.documentElement;

    // Set the lang attribute to the provided culture code
    htmlElement.setAttribute('lang', culture);
}

window.removeNode = (elementId) => {
    var domNode = document.getElementById(elementId);
    if (domNode) {
        domNode.remove();
    }
}

async function lazyLoadScript(url) {
    return new Promise(function (resolve, reject) {
        // Check if the script is already loaded
        if (document.getElementById(url)) {
            resolve(); // Already loaded, resolve the promise
            return;
        }

        var script = document.createElement('script');
        script.src = url; // Path to your JavaScript file
        script.id = url;

        // Resolve the promise when the script loads successfully
        script.onload = function () {
            resolve();
        };

        // Reject the promise if there's an error
        script.onerror = function () {
            reject(new Error('Failed to load the script.'));
        };

        document.body.appendChild(script);
    });
}

window.AddScriptElement = async (url) => {
    try {
        await lazyLoadScript(url);
    } catch (e) {
        console.log(e)
    }
}

// override reload 

let startY = 0;
let isPulling = false;
const refreshIcon = document.getElementById('refreshIcon');

window.addEventListener('touchstart', (e) => {
    if (window.scrollY === 0) {
        startY = e.touches[0].clientY;
        isPulling = true;
    }
});

window.addEventListener('touchmove', (e) => {
    if (isPulling) {
        const moveY = e.touches[0].clientY - startY;
        if (moveY > 0) {
            refreshIcon.style.top = `${Math.min(moveY - 50, 20)}px`;
        }
    }
});

window.addEventListener('touchend', () => {
    if (isPulling) {
        isPulling = false;
        if (parseInt(refreshIcon.style.top) > 10) {
            refreshIcon.style.top = '-50px';
            try {
                DotNet.invokeMethodAsync('WebApp.Client', 'ScrollToReloadEventListener')
                    .then(data => {
                        if (data) {
                            location.reload();
                        }
                    })
                    .catch(e => console.log(e));
            } catch (error) {
                location.reload();
            }

        } else {
            refreshIcon.style.top = '-50px';
        }
    }
});


document.documentElement.setAttribute('lang', window.getCultureFromCookie());