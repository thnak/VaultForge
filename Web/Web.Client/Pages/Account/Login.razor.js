// script.js
function createFirework() {
    const fireworkContainer = document.getElementById('firework-container');
    const firework = document.createElement('div');
    firework.className = 'firework';

    const size = Math.random() * 10 + 5;
    firework.style.width = `${size}px`;
    firework.style.height = `${size}px`;

    const leftPosition = Math.random() * 100;
    firework.style.left = `${leftPosition}vw`;

    const animationDuration = Math.random() * 5 + 5;
    firework.style.animationDuration = `${animationDuration}s`;

    fireworkContainer.appendChild(firework);

    setTimeout(() => {
        firework.remove();
    }, animationDuration * 1000);
}

function initFireworks() {
    setInterval(createFirework, 1000);
}

function ForceLogin() {
    var btn = document.getElementById("login-btn");
    btn.click();
}

document.addEventListener('DOMContentLoaded', initFireworks);
