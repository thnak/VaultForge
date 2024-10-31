export class HlsVideoPlayer {
    constructor(videoElementId, videoUrl) {
        this.videoElementId = videoElementId;
        this.videoUrl = videoUrl;
        this.videoPlayer = document.getElementById(this.videoElementId);
        this.hls = null;

        if (!this.videoPlayer) {
            console.error(`Video element with id ${this.videoElementId} not found.`);
            return;
        }

        // Initialize MutationObserver
        this.initMutationObserver();
        // Setup player
        this.setupPlayer();
    }

    initMutationObserver() {
        const observerConfig = { attributes: true, childList: false, subtree: false };

        this.observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'attributes') {
                    console.log(`Attribute ${mutation.attributeName} changed.`);
                    // Re-setup the player if certain attributes change
                    this.setupPlayer();
                }
            });
        });

        this.observer.observe(this.videoPlayer, observerConfig);
    }

    setupPlayer() {
        // Check for HLS support
        if (Hls.isSupported()) {
            this.setupHlsPlayer();
        } else if (this.videoPlayer.canPlayType('application/vnd.apple.mpegurl')) {
            this.setupNativePlayer();
        } else {
            console.warn("HLS not supported on this browser.");
        }
    }

    setupHlsPlayer() {
        if (this.hls) {
            this.hls.destroy();
        }

        this.hls = new Hls();
        this.hls.loadSource(this.videoUrl);
        this.hls.attachMedia(this.videoPlayer);

        this.hls.on(Hls.Events.MANIFEST_PARSED, () => {
            this.videoPlayer.play();
        });
    }

    setupNativePlayer() {
        this.videoPlayer.src = this.videoUrl;
        this.videoPlayer.addEventListener('loadedmetadata', () => {
            this.videoPlayer.play();
        });
    }

    // Method to stop observing changes
    disconnectObserver() {
        if (this.observer) {
            this.observer.disconnect();
        }
    }
}

window.HlsVideoPlayer = HlsVideoPlayer;