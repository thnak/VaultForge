window.labelDrawerContainerHelper = (() => {

    let canvas = null;
    let upgradeCallbackHandler = null;
    let ctx = null;

    let image = new Image();
    let scale = 1.0;
    let offsetX = 0;
    let offsetY = 0;
    let isDragging = false;
    let isPanning = false;
    let startX, startY, endX, endY;
    let lastPanX, lastPanY;
    let boundingBoxes = [];


    let mouseX = 0;
    let mouseY = 0;
    let showCrosshair = true; // Toggle to enable/disable the crosshair

// Load and draw the image
    image.onload = () => {
        draw();
    };

    function SetRefHandler(dotnetRef, canvasId, imageSrc) {
        upgradeCallbackHandler = dotnetRef;
        canvas = document.getElementById(canvasId);
        canvas.width = canvas.parentElement.offsetWidth;
        canvas.height = canvas.parentElement.offsetHeight;
        image.src = imageSrc; // Replace with your image path
        ctx = canvas.getContext('2d');
    }


// Draw everything on the canvas
    function draw() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.save();

        // Apply transformations
        ctx.translate(offsetX, offsetY);
        ctx.scale(scale, scale);

        // Draw image
        ctx.drawImage(image, 0, 0, image.width, image.height);

        // Draw bounding boxes
        ctx.strokeStyle = 'red';
        ctx.lineWidth = 2 / scale;
        boundingBoxes.forEach(box => {
            ctx.strokeRect(box.x, box.y, box.width, box.height);
        });

        ctx.restore();
        // Draw crosshair
        if (showCrosshair) {
            drawCrosshair();
        }
    }

// Draw the crosshair
    function drawCrosshair() {
        ctx.save();
        ctx.strokeStyle = 'green';
        ctx.lineWidth = 1;
        const {x, y} = getTransformedPoint(mouseX, mouseY);

        // Horizontal line
        ctx.beginPath();
        ctx.moveTo(0, mouseY);
        ctx.lineTo(canvas.width, mouseY);
        ctx.stroke();

        // Vertical line
        ctx.beginPath();
        ctx.moveTo(mouseX, 0);
        ctx.lineTo(mouseX, canvas.height);
        ctx.stroke();

        // Circle at intersection (optional)
        ctx.beginPath();
        ctx.arc(mouseX, mouseY, 5, 0, 2 * Math.PI);
        ctx.fillStyle = 'green';
        ctx.fill();
        ctx.restore();
    }

// Handle mouse down
    canvas.addEventListener('mousedown', (e) => {
        const {x, y} = getTransformedPoint(e.offsetX, e.offsetY);

        if (e.ctrlKey) {
            // Start panning
            isPanning = true;
            lastPanX = e.offsetX;
            lastPanY = e.offsetY;
        } else if (isPointInImage(x, y)) {
            // Start drawing a bounding box (only if within the image)
            startX = x;
            startY = y;
            isDragging = true;
        }
    });

// Handle mouse move
    canvas.addEventListener('mousemove', (e) => {
        mouseX = e.offsetX;
        mouseY = e.offsetY;
        draw();

        if (isPanning) {
            // Panning logic
            const dx = e.offsetX - lastPanX;
            const dy = e.offsetY - lastPanY;

            offsetX += dx;
            offsetY += dy;

            lastPanX = e.offsetX;
            lastPanY = e.offsetY;

        } else if (isDragging) {
            // Drawing bounding box logic
            const {x, y} = getTransformedPoint(e.offsetX, e.offsetY);

            // Clamp endX and endY to ensure the box stays within the image
            endX = Math.min(Math.max(0, x), image.width);
            endY = Math.min(Math.max(0, y), image.height);

            draw();

            // Draw the current bounding box on canvas, adjusting for offsets
            ctx.save();
            ctx.strokeStyle = 'blue';
            ctx.lineWidth = 2 / scale;
            ctx.strokeRect(
                startX * scale + offsetX,
                startY * scale + offsetY,
                (endX - startX) * scale,
                (endY - startY) * scale
            );
            ctx.restore();
        }
    });


// Handle mouse up
    canvas.addEventListener('mouseup', () => {
        if (isDragging) {
            isDragging = false;

            // Ensure bounding box is within image boundaries
            const width = Math.min(image.width, endX) - Math.min(image.width, startX);
            const height = Math.min(image.height, endY) - Math.min(image.height, startY);

            if (width > 0 && height > 0) {
                boundingBoxes.push({
                    x: Math.min(startX, endX),
                    y: Math.min(startY, endY),
                    width,
                    height,
                });
            }
        } else if (isPanning) {
            isPanning = false;
        }

        draw();
        if (upgradeCallbackHandler) {
            upgradeCallbackHandler.invokeMethodAsync("ReceiveBoundingBoxes", boundingBoxes);
        }
    });

// Zoom with Ctrl + Scroll
    canvas.addEventListener('wheel', (e) => {
        if (e.ctrlKey) {
            e.preventDefault();

            const zoomFactor = 1.1;
            const mousePos = getTransformedPoint(e.offsetX, e.offsetY);

            if (e.deltaY < 0) {
                scale *= zoomFactor;
            } else {
                scale /= zoomFactor;
            }

            // Clamp zoom to prevent excessive zooming
            scale = Math.min(Math.max(scale, 0.5), 5);

            // Adjust offset to keep the zoom centered
            offsetX -= (mousePos.x * (scale / zoomFactor - scale)) * zoomFactor;
            offsetY -= (mousePos.y * (scale / zoomFactor - scale)) * zoomFactor;

            draw();
        }
    });

// Helper: Get transformed point
    function getTransformedPoint(x, y) {
        return {
            x: (x - offsetX) / scale,
            y: (y - offsetY) / scale,
        };
    }

// Helper: Check if a point is within the image boundaries
    function isPointInImage(x, y) {
        return x >= 0 && x <= image.width && y >= 0 && y <= image.height;
    }

    return {SetRefHandler};
})();