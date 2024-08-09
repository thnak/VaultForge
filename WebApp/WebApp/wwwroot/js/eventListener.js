const assemblyName = 'Web.Client';
let preventKey = [];

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


function PageShowEvent() {
    DotNet.invokeMethodAsync(assemblyName, 'PageShowEventEventListener')
}

function PageHideEvent() {
    DotNet.invokeMethodAsync(assemblyName, 'PageHideEventEventListener')
}

function ContextMenuEvent(e) {
    e.preventDefault();
    DotNet.invokeMethodAsync(assemblyName, 'ContextMenuEventListener')
    DotNet.invokeMethodAsync(assemblyName, 'ContextMenuEventListenerWithParam', e.clientX, e.clientY)
}

function KeyDownEvent(event) {
    var keyPrevent = localStorage.getItem("PreventKey");
    if(keyPrevent !== null && keyPrevent.includes(event.code))
        event.preventDefault();
    
    if (event.keyCode === 122) {
        FullScreenEvent();
    }

    DotNet.invokeMethodAsync(assemblyName, 'KeyPressChangeEventListener', event.code);
}

function OfflineEvent() {
    DotNet.invokeMethodAsync(assemblyName, 'OfflineEventListener')

}

function OnlineEvent() {
    DotNet.invokeMethodAsync(assemblyName, 'OnlineEventListener')
}

let visibilityChange;
if (typeof document.hidden !== "undefined") {
    visibilityChange = "visibilitychange";
} else if (typeof document.mozHidden !== "undefined") { // Firefox up to v17
    visibilityChange = "mozvisibilitychange";
} else if (typeof document.webkitHidden !== "undefined") { // Chrome up to v32, Android up to v4.4, Blackberry up to v10
    visibilityChange = "webkitvisibilitychange";
}

function VisibilitychangeEvent() {
    DotNet.invokeMethodAsync(assemblyName, 'VisibilityChangeEventListener', document.hidden === false)
}

function AppInstalledEvent() {
    DotNet.invokeMethodAsync(assemblyName, 'InstalledEventListener')
}

function FullScreenEvent() {
    DotNet.invokeMethodAsync(assemblyName, 'FullScreenChangeEventListener', document.fullscreenElement != null || window.innerHeight === screen.height)
}

function PageChangeSize() {
    FullScreenEvent();
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
}