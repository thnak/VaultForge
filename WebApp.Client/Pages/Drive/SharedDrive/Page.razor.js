const ASSEMBLY_NAME = "WebApp.Client";
let lastUpdateTime = 0;

export function getMessage() {
    return 'Olá do Blazor!';
}

function _(el) {
    return document.getElementById(el);
}


/**
 * Adds two numbers
 * @param {string} api
 * @param folder
 */
export async function uploadFile(api, folder) {
    const files = document.getElementById("file1").files;
    const formdata = new FormData();
    for (var i = 0; i < files.length; i++) {
        formdata.append(`file ${i}`, files[i], files[i].name);
    }

    var ajax = new XMLHttpRequest();
    ajax.open("POST", api, true);
    ajax.setRequestHeader("Folder", folder)
    ajax.upload.addEventListener("progress", progressHandler);
    ajax.addEventListener("readystatechange", completeHandler);
    ajax.addEventListener("error", errorHandler);
    ajax.addEventListener("abort", abortHandler);
    ajax.send(formdata);
}

export function getSelectedFiles() {
    const files = document.getElementById("file1").files;
    const formdata = new FormData();
    const fileNameList = [];
    for (var i = 0; i < files.length; i++) {
        formdata.append(`file ${i}`, files[i], files[i].name);
        fileNameList.push(files[i].name);
    }

    return fileNameList;
}

export function getSelectedFileSize() {
    const files = document.getElementById("file1").files;
    const formdata = new FormData();
    const fileNameList = [];
    for (var i = 0; i < files.length; i++) {
        formdata.append(`file ${i}`, files[i], files[i].name);
        fileNameList.push(files[i].size);
    }

    return fileNameList;
}

export function clearSelectedFiles() {
    document.getElementById("file1").value = '';
}

/**
 *
 * @param {ProgressEvent} event
 */
async function progressHandler(event) {
    const currentTime = Date.now();
    if (event.lengthComputable && (currentTime - lastUpdateTime) >= 50) {
        await DotNet.invokeMethodAsync(ASSEMBLY_NAME, "UpFileLoadProgressJsInvoke", [event.total, event.loaded])
        lastUpdateTime = currentTime;
    }
}

/**
 *
 * @param {ProgressEvent} event
 */
async function completeHandler(event) {
    if (event.target.readyState === 4 || event.target.readyState === "complete") {
        // The document and all sub-resources have finished loading.
        await DotNet.invokeMethodAsync(ASSEMBLY_NAME, 'OnComplete', event.target.status, event.target.responseText);
    }
}

/**
 *
 * @param {ProgressEvent<XMLHttpRequestEventTarget>} event
 */
async function errorHandler(event) {
    await DotNet.invokeMethodAsync(ASSEMBLY_NAME, 'OnError', event.target.status, event.target.responseText);
}

/**
 *
 * @param {ProgressEvent<XMLHttpRequestEventTarget>} event
 */
async function abortHandler(event) {
    await DotNet.invokeMethodAsync(ASSEMBLY_NAME, 'OnError', "", "Aborted");
}