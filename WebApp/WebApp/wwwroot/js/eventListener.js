const assemblyName = 'WebApp.Client';

window.deleteTemplateCache = () => {
    caches.keys().then(function (names) {
        for (let name of names) caches.delete(name);
    });
}

window.Download = (url) => {
    const ele = document.createElement("a");
    ele.href = url;
    ele.classList.add('d-none');
    document.body.appendChild(ele);
    ele.click();
    document.body.removeChild(ele);
}


window.getCookie = (cname) => {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) === ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) === 0) {
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

window.RequestFullScreen = () => {
    var elem = document.documentElement;
    if (elem.requestFullscreen) {
        elem.requestFullscreen();
    } else if (elem.msRequestFullscreen) {
        elem.msRequestFullscreen();
    } else if (elem.mozRequestFullScreen) {
        elem.mozRequestFullScreen();
    } else if (elem.webkitRequestFullscreen) {
        elem.webkitRequestFullscreen();
    }
}


async function PageShowEvent() {
    await DotNet.invokeMethodAsync(assemblyName, 'PageShowEventEventListener')
}

async function PageHideEvent() {
    await DotNet.invokeMethodAsync(assemblyName, 'PageHideEventEventListener')
}

async function ContextMenuEvent(e) {
    var keyPrevent = localStorage.getItem("PreventKey");
    if (keyPrevent.includes("contextmenu")) {
        e.preventDefault();
    }
    await DotNet.invokeMethodAsync(assemblyName, 'ContextMenuEventListener')
    await DotNet.invokeMethodAsync(assemblyName, 'ContextMenuEventListenerWithParam', e.clientX, e.clientY)
}

async function KeyDownEvent(event) {
    var keyPrevent = localStorage.getItem("PreventKey");
    if (keyPrevent !== null && keyPrevent.includes(event.code))
        event.preventDefault();

    if (event.keyCode === 122) {
        await FullScreenEvent();
    }

    await DotNet.invokeMethodAsync(assemblyName, 'KeyPressChangeEventListener', event.code);
}

async function OfflineEvent() {
    await DotNet.invokeMethodAsync(assemblyName, 'OfflineEventListener')

}

async function OnlineEvent() {
    await DotNet.invokeMethodAsync(assemblyName, 'OnlineEventListener')
}

let visibilityChange;
if (typeof document.hidden !== "undefined") {
    visibilityChange = "visibilitychange";
} else if (typeof document.mozHidden !== "undefined") { // Firefox up to v17
    visibilityChange = "mozvisibilitychange";
} else if (typeof document.webkitHidden !== "undefined") { // Chrome up to v32, Android up to v4.4, Blackberry up to v10
    visibilityChange = "webkitvisibilitychange";
}

async function VisibilitychangeEvent() {
    await DotNet.invokeMethodAsync(assemblyName, 'VisibilityChangeEventListener', document.hidden === false)
}

async function AppInstalledEvent() {
    await DotNet.invokeMethodAsync(assemblyName, 'InstalledEventListener')
}

async function FullScreenEvent() {
    await DotNet.invokeMethodAsync(assemblyName, 'FullScreenChangeEventListener', document.fullscreenElement != null || window.innerHeight === screen.height)
}

async function PageChangeSize() {
    await FullScreenEvent();
}

//
// Events Listener
//

window.InitAppEventListener = () => {
    window.addEventListener("pagehide", PageHideEvent);
    window.addEventListener("pageshow", PageShowEvent);
    window.addEventListener("contextmenu", ContextMenuEvent);
    window.addEventListener("keydown", KeyDownEvent);
    window.addEventListener('online', OnlineEvent);
    window.addEventListener('offline', OfflineEvent);
    window.addEventListener(visibilityChange, VisibilitychangeEvent);
    window.addEventListener("appinstalled", AppInstalledEvent);
    document.addEventListener("fullscreenchange", FullScreenEvent);
    window.addEventListener('resize', PageChangeSize);
    console.log("Init event listener");
    DotNet.invokeMethodAsync(assemblyName, 'TouchEventListenerAsync', "ontouchstart" in document.documentElement);
}