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

window.AddScriptElement = (url) => {
    const script = document.createElement('script');
    script.src = url;
    script.type = 'text/javascript';
    script.async = false; // Optional: load asynchronously
  //  document.head.appendChild(script); // 
}

document.documentElement.setAttribute('lang', window.getCultureFromCookie());