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

    async function addFile(dbName, storeName, stream, contentType, title) {
        return new Promise(async (resolve, reject) => {
            const db = databases[dbName];
            if (!db) return reject(new Error(`Database ${dbName} not opened`));

            let transaction;
            try {
                transaction = db.transaction(storeName, "readwrite");
                const store = transaction.objectStore(storeName);

                const arrayBuffer = await stream.arrayBuffer();
                let blobOptions = {};
                if (contentType) {
                    blobOptions['type'] = contentType;
                }
                const blob = new Blob([arrayBuffer], blobOptions);

                // Generate a unique identifier (e.g., using UUIDs)

                const fileData = {
                    fileId: title,
                    file: blob
                };

                const request = store.add(fileData);

                request.onsuccess = () => {
                    transaction.commit();
                    resolve(fileId); // Resolve with the fileId
                };
                request.onerror = (event) => {
                    transaction.abort(); // Abort the transaction on error
                    reject(new Error(`Error adding file: ${event.target.error}`));
                };

            } catch (error) {
                if (transaction) {
                    transaction.abort();
                }
                reject(error);
            }
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

