window.indexedDbHelper = (() => {
    const databases = {};

    function createStore(dbName, storeName, version, keyPath, upgradeCallbackHandler) {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(dbName, version);

            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                if (!db.objectStoreNames.contains(storeName)) {
                    db.createObjectStore(storeName, { keyPath: keyPath });
                }

                if (upgradeCallbackHandler) {
                    const oldVersion = event.oldVersion || 0;
                    const newVersion = event.newVersion;
                    upgradeCallbackHandler.invokeMethodAsync("UpgradeCallback", oldVersion, newVersion).catch(console.error);
                }
            };

            request.onsuccess = () => {
                databases[dbName] = request.result;
                resolve({ status: "success", dbName: dbName });
            };

            request.onerror = () => {
                reject({ status: "error", error: request.error.message });
            };
        });
    }

    async function addItem(dbName, storeName, item) {
        return new Promise((resolve, reject) => {
            const db = databases[dbName];
            if (!db) return reject(new Error(`Database ${dbName} not opened`));

            const transaction = db.transaction(storeName, "readwrite");
            const store = transaction.objectStore(storeName);

            const request = store.add(item);

            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }

    async function addFile(dbName, storeName, array) {
        return new Promise((resolve, reject) => {
            const db = databases[dbName];
            if (!db) return reject(new Error(`Database ${dbName} not opened`));

            const transaction = db.transaction(storeName, "readwrite");
            const store = transaction.objectStore(storeName);

            const blob = new Blob([array], { type: 'application/octet-stream' });
            const fileData = {
                fileId: fileId,  // unique identifier for the file
                file: blob
            };
            const request = store.add(fileData);

            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }

    async function getItem(dbName, storeName, id) {
        return new Promise((resolve, reject) => {
            const db = databases[dbName];
            if (!db) return reject(new Error(`Database ${dbName} not opened`));

            const transaction = db.transaction(storeName, "readonly");
            const store = transaction.objectStore(storeName);

            const request = store.get(id);

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    function deleteItem(dbName, storeName, id) {
        return new Promise((resolve, reject) => {
            const db = databases[dbName];
            if (!db) {
                reject("Database not found");
                return;
            }
            const transaction = db.transaction(storeName, "readwrite");
            const store = transaction.objectStore(storeName);
            const request = store.delete(id);

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    }

    return {createStore, addItem, getItem, addFile, deleteItem};
})();

