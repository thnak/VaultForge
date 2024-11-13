const imageUpload = document.getElementById('imageUpload');
const canvas = document.getElementById('annotationCanvas');
const ctx = canvas.getContext('2d');
const img = new Image();

const defaultPointerColor = "green";
const defaultLineColor = "green";

let isDrawing = false;
let isResizing = false;
let isDragging = false;
let startX, startY, rectWidth, rectHeight;
let rectangles = [];
let currentRectangle = null;

let handleSize = 10;
let scale = 1;
let originX = 0;
let originY = 0;

let originOffsetX = 0;
let originOffsetY = 0;
let movingImage = false;

let originHeight = 0;
let originWidth = 0;


imageUpload.addEventListener('change', handleImageUpload);
canvas.addEventListener('mousedown', handleMouseDown);
canvas.addEventListener('mousemove', handleMouseMove);
canvas.addEventListener('mouseup', handleMouseUp);
canvas.addEventListener('contextmenu', handleContextMenu);
canvas.addEventListener('wheel', handleWheel);

function handleImageUpload(event) {
    const file = event.target.files[0];
    const reader = new FileReader();

    reader.onload = function (e) {
        img.onload = function () {
            ResetVavlues();

            const container = document.getElementById('container');
            const maxWidth = container.clientWidth;
            const maxHeight = container.clientHeight;
            let width = img.width;
            let height = img.height;
            originHeight = img.height;
            originWidth = img.width;

            if (width > maxWidth) {
                height *= maxWidth / width;
                width = maxWidth;
            }
            if (height > maxHeight) {
                width *= maxHeight / height;
                height = maxHeight;
            }

            height = Math.floor(height);
            width = Math.floor(width);

            canvas.width = width;
            canvas.height = height;

            scale = width / img.width;
            originX = Math.floor(canvas.width - width) / 2;
            originY = Math.floor(canvas.height - height) / 2;

            CaculateHandleSize();

            ctx.drawImage(img, 0, 0, width, height);
            redrawCanvas();
        }
        img.src = e.target.result;
    }
    reader.readAsDataURL(file);
}

function ResetVavlues() {

    isDrawing = false;

    isResizing = false;

    isDragging = false;
    movingImage = false;
    startX, startY, rectWidth, rectHeight = 0, 0, 0, 0;

    rectangles = [];

    currentRectangle = null;
    handleSize = 10;

    scale = 1;

    originX = 0;

    originY = 0;
}

function CaculateHandleSize() {
    var min = Math.min(canvas.height, canvas.width);
    handleSize = Math.max(min * 0.05, 10);
    handleSize = Math.min(handleSize, 20);
}

function getRealHandleSize() {
    var canvaVmin = Math.min(canvas.height, canvas.width);
    var imageVmin = Math.min(img.height, img.width);

    return Math.max(imageVmin * handleSize / canvaVmin, handleSize);
}

function handleMouseDown(e) {
    if (e.button !== 0) return; // Only respond to left-click
    if (e.ctrlKey) {
        originOffsetX = originX + canvas.width / 2;
        originOffsetY = originY + canvas.height / 2;
        movingImage = true;
        return;
    }
    startX = (e.offsetX - originX) / scale;
    startY = (e.offsetY - originY) / scale;
    currentRectangle = getRectangleAtPoint(startX, startY);

    if (currentRectangle) {
        const handle = getHandleAtPoint(startX, startY);
        if (handle) {
            isResizing = true;
        } else {
            isDragging = true;
        }
    } else {
        isDrawing = true;
    }
    console.log(rectangles);
}

function handleMouseMove(e) {
    if (e.ctrlKey) {
        if (movingImage) {
            e.preventDefault();
            originX = originOffsetX - e.offsetX;
            originY = originOffsetY - e.offsetY;
            CaculateHandleSize();
            redrawCanvas();
            return;
        }
    }
    let mouseX = (e.offsetX - originX) / scale;
    let mouseY = (e.offsetY - originY) / scale;
    if (mouseX < 0)
        mouseX = 0;
    if (mouseY < 0)
        mouseY = 0;

    if (mouseX > originWidth)
        mouseX = originWidth;
    if (mouseY > originHeight)
        mouseY = originHeight;
    if (originWidth - mouseX < 0)
        mouseX = 0;
    if (originHeight - mouseY < 0)
        mouseY = 0;


    if (isDrawing) {

        rectWidth = mouseX - startX;
        rectHeight = mouseY - startY;
        redrawCanvas();
        drawRectangle(startX, startY, rectWidth, rectHeight);
        drawPointer(e.offsetX, e.offsetY, 'white', 'white');
    } else if (isResizing && currentRectangle) {
        const handle = getHandleAtPoint(mouseX, mouseY);
        if (handle) {
            resizeRectangle(handle, mouseX, mouseY);
            redrawCanvas();
            drawPointer(e.offsetX, e.offsetY, 'red', 'red');
        }
    } else if (isDragging && currentRectangle) {
        moveRectangle(currentRectangle, mouseX - startX, mouseY - startY);
        startX = mouseX;
        startY = mouseY;
        redrawCanvas();
        drawPointer(e.offsetX, e.offsetY, 'orange', 'orange');
    } else {
        if (getRectangleAtPoint(mouseX, mouseY)) {
            if (getHandleAtPoint(mouseX, mouseY)) {
                redrawCanvas();
                drawPointer(e.offsetX, e.offsetY, 'red', 'red');
                return;
            }
        }

        redrawCanvas();
        if (mouseX < originWidth && mouseY < originHeight)
            if (mouseX > 0 && mouseY > 0)
                drawPointer(e.offsetX, e.offsetY, defaultPointerColor, defaultLineColor);
    }
}

function handleMouseUp() {
    if (isDrawing) {
        if (rectWidth < 0) {
            startX += rectWidth;
            rectWidth = Math.abs(rectWidth);
        }
        if (rectHeight < 0) {
            startY += rectHeight;
            rectHeight = Math.abs(rectHeight);
        }

        if (startX + rectWidth > originWidth) {
            rectWidth = originWidth - startX;
        }
        if (startY + rectHeight > originHeight) {
            rectHeight = originHeight - startY;
        }

        if (startX + rectWidth > originWidth) {
            rectWidth = originWidth - startX;
        }
        if (startY + rectHeight > originHeight) {
            rectHeight = originHeight - startX;
        }

        if (rectWidth < 0) {
            startX += rectWidth;
            rectWidth = Math.abs(rectWidth);
        }
        if (rectHeight < 0) {
            startY += rectHeight;
            rectHeight = Math.abs(rectHeight);
        }

        if (startX < 0 || startY < 0 || rectHeight <= 10 || rectWidth <= 10 || typeof (rectHeight) === undefined || typeof (rectWidth) === undefined) {
            redrawCanvas();
        } else {

            rectangles.push({x: startX, y: startY, width: rectWidth, height: rectHeight});
            redrawCanvas();
        }

        isDrawing = false;
    }
    isResizing = false;
    isDragging = false;
    movingImage = false;
    currentRectangle = null;
}

function handleContextMenu(e) {
    e.preventDefault();
    const x = (e.offsetX - originX) / scale;
    const y = (e.offsetY - originY) / scale;
    const rect = getRectangleAtPoint(x, y);
    if (rect) {
        rectangles = rectangles.filter(r => r !== rect);
        redrawCanvas();
    }
}

function handleWheel(e) {
    if (e.ctrlKey) {
        e.preventDefault();
        const delta = e.deltaY < 0 ? 1.1 : 0.9;
        const prevScale = scale;
        scale *= delta;

        // Adjust origin to zoom towards the cursor
        originX = e.offsetX - (e.offsetX - originX) * (scale / prevScale);
        originY = e.offsetY - (e.offsetY - originY) * (scale / prevScale);
        CaculateHandleSize();
        redrawCanvas();
    }
}

function redrawCanvas() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, originX, originY, img.width * scale, img.height * scale);
    rectangles.forEach(rect => drawRectangle(rect.x, rect.y, rect.width, rect.height));
}

function drawPointer(x, y, pointColor, lineColor) {
    ctx.beginPath();
    ctx.strokeStyle = lineColor;
    ctx.moveTo(x + 10, y);
    ctx.lineTo(canvas.width, y);
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(x - 10, y);
    ctx.lineTo(0, y);
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(x, y - 10);
    ctx.lineTo(x, 0);
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(x, y + 10);
    ctx.lineTo(x, canvas.height);
    ctx.stroke();

    ctx.beginPath();
    ctx.fillStyle = pointColor;
    ctx.fillRect(x - 5, y - 5, 10, 10);
    ctx.stroke();
}

function drawRectangle(x, y, width, height) {
    ctx.beginPath();
    ctx.strokeStyle = 'blue';
    ctx.fillStyle = "rgba(255, 255, 255, 0.5)"
    ctx.fillRect(x * scale + originX, y * scale + originY, width * scale, height * scale);
    drawHandles(x, y, width, height);
    ctx.stroke();
}

function drawHandles(x, y, width, height) {
    const handles = getHandles(x, y, width, height);
    ctx.fillStyle = 'blue';
    handles.forEach(handle => {
        ctx.fillRect(handle.x * scale + originX - handleSize / 2, handle.y * scale + originY - handleSize / 2, handleSize, handleSize);
    });
}

function getHandles(x, y, width, height) {
    return [
        {x: x, y: y},
        {x: x + width, y: y},
        {x: x, y: y + height},
        {x: x + width, y: y + height}
    ];
}

function getHandleAtPoint(x, y) {
    var actualHandleSize = isResizing ? getRealHandleSize() / 4 : getRealHandleSize() / 2;
    for (const rect of rectangles) {
        const handles = getHandles(rect.x, rect.y, rect.width, rect.height);
        for (const handle of handles) {

            if (x >= handle.x - actualHandleSize && x <= handle.x + actualHandleSize &&
                y >= handle.y - actualHandleSize && y <= handle.y + actualHandleSize) {
                return handle;
            }
        }
    }
    return null;
}

function getRectangleAtPoint(x, y) {
    var actualHandleSize = getRealHandleSize() / 2;
    for (const rect of rectangles) {
        if (x >= rect.x - actualHandleSize && x <= rect.x + rect.width + actualHandleSize &&
            y >= rect.y - actualHandleSize && y <= rect.y + rect.height + actualHandleSize) {
            return rect;
        }
    }
    return null;
}

function resizeRectangle(handle, x, y) {
    const rect = currentRectangle;
    if (handle.x === rect.x && handle.y === rect.y) {
        rect.width += rect.x - x;
        rect.height += rect.y - y;
        rect.x = x;
        rect.y = y;
    } else if (handle.x === rect.x + rect.width && handle.y === rect.y) {
        rect.width = x - rect.x;
        rect.height += rect.y - y;
        rect.y = y;
    } else if (handle.x === rect.x && handle.y === rect.y + rect.height) {
        rect.width += rect.x - x;
        rect.height = y - rect.y;
        rect.x = x;
    } else if (handle.x === rect.x + rect.width && handle.y === rect.y + rect.height) {
        rect.width = x - rect.x;
        rect.height = y - rect.y;
    }
    if (rect.width < 0) {
        rect.x += rect.width;
        rect.width = Math.abs(rect.width);
    }
    if (rect.height < 0) {
        rect.y += rect.height;
        rect.height = Math.abs(rect.height);
    }

    if (x < originX) {
        x = originX;
    }
}

function moveRectangle(rect, dx, dy) {
    rect.x += dx;
    rect.y += dy;

    // Prevent the rectangle from moving out of canvas bounds
    if (rect.x < 0) rect.x = 0;
    if (rect.y < 0) rect.y = 0;

    if (rect.x + rect.width > originWidth)
        rect.x = originWidth - rect.width;
    if (rect.y + rect.height > originHeight)
        rect.y = originHeight - rect.height;
}

