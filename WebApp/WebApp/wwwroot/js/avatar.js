window.previewImage = function () {
    var input = document.getElementById('avatar');
    var canvas = document.getElementById('canvas');
    var reader = new FileReader();

    reader.onload = function (e) {
        var img = new Image();
        img.onload = function () {
            var ctx = canvas.getContext('2d');
            canvas.width = 150;
            canvas.height = 150;
            ctx.drawImage(img, 0, 0, 150, 150);

            canvas.style.display = 'block';
        }
        img.src = e.target.result;
    };

    reader.readAsDataURL(input.files[0]);
};

window.getResizedImage = function (dotNetObject) {
    var canvas = document.getElementById('canvas');
    if (!canvas) return;

    var resizedImageDataUrl = canvas.toDataURL('image/png');
    dotNetObject.invokeMethodAsync('SetResizedImage', resizedImageDataUrl);
};