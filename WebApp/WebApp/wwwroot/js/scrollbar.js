// Get the scrollbar and initialize a timeout variable
let customScrollbar = document.createElement('div');
customScrollbar.id = 'custom-scrollbar';
document.body.appendChild(customScrollbar);

// Create the thumb element for the scrollbar
let thumb = document.createElement('div');
thumb.classList.add('thumb');
customScrollbar.appendChild(thumb);

let hideTimeout;
let isDragging = false; // Flag to track if the user is dragging
let startY; // Variable to store the initial Y position when dragging starts
let startScrollTop; // Variable to store the initial scroll position when dragging starts

// Function to start dragging
thumb.addEventListener('mousedown', (e) => {
    isDragging = true;
    startY = e.clientY; // Get the initial Y position when the drag starts
    startScrollTop = window.scrollY; // Get the current scroll position

    // Prevent text selection or other default behaviors while dragging
    document.body.style.userSelect = 'none';

    // Show the scrollbar immediately
    customScrollbar.style.opacity = '1';
});

// Function to handle the dragging movement
document.addEventListener('mousemove', (e) => {
    if (isDragging) {
        const deltaY = e.clientY - startY; // Calculate the movement distance
        const contentHeight = document.documentElement.scrollHeight;
        const viewportHeight = window.innerHeight;

        // Calculate the new scroll position based on the movement
        const scrollAmount = deltaY / viewportHeight * contentHeight;

        // Scroll the page by the calculated amount
        window.scrollTo(0, startScrollTop + scrollAmount);
    }
});

// Function to stop dragging
document.addEventListener('mouseup', () => {
    if (isDragging) {
        isDragging = false;

        // Re-enable text selection after dragging ends
        document.body.style.userSelect = '';

        // Hide the scrollbar after 1 second of inactivity
        hideTimeout = setTimeout(() => {
            customScrollbar.style.opacity = '0';
        }, 1000);
    }
});

// Update the scrollbar position and size (same as before)
function updateScrollbar() {
    const contentHeight = document.documentElement.scrollHeight;
    const viewportHeight = window.innerHeight;
    const scrollTop = window.scrollY;
    const thumbHeight = Math.max(viewportHeight / contentHeight * viewportHeight, 30); // Set minimum thumb height

    thumb.style.height = `${thumbHeight}px`;
    thumb.style.top = `${scrollTop / contentHeight * viewportHeight}px`;

    // Show the scrollbar
    customScrollbar.style.opacity = '1';

    // Clear previous timeout if the user is still scrolling
    if (!isDragging && hideTimeout) clearTimeout(hideTimeout);

    // Hide the scrollbar after 1 second of inactivity (only if not dragging)
    if (!isDragging) {
        hideTimeout = setTimeout(() => {
            customScrollbar.style.opacity = '0';
        }, 1000);
    }
}

// Add event listeners for scrolling and resizing the window
window.addEventListener('scroll', updateScrollbar);
window.addEventListener('resize', updateScrollbar);

// Initial scrollbar update
updateScrollbar();
