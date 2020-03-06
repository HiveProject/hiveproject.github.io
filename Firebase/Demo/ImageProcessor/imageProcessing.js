const maxDimention = 800;

function scaleToMaxSize(b64) {
    return new Promise((res, rej) => {
        var img = new Image();
        let canvas = document.createElement("canvas");
        let context = canvas.getContext('2d');
        img.onload = function () {
            let width = img.width;
            let height = img.height;
            let scale = Math.min(maxDimention / width, maxDimention / height);
            if (scale > 1) scale = 1;
            width *= scale;
            height *= scale;
            canvas.width = width;
            canvas.height = height;
            context.drawImage(img, 0, 0, width, height);
            res(canvas.toDataURL("image/png"));
        };
        img.src = b64;
    });
}
function getImageData(b64) {
    return new Promise((res, rej) => {
        let image = new Image();
        let canvas = document.createElement("canvas");
        let context = canvas.getContext('2d');

        image.onload = function () {
            let width = image.width;
            let height = image.height;
            canvas.width = width;
            canvas.height = height;
            context.drawImage(image, 0, 0);
            var imageData = context.getImageData(0, 0, width, height);

            res(imageData);
        };
        image.src = b64;

    });
}
function getBase64(imageData) {
    return new Promise((res, rej) => {
        let canvas = document.createElement("canvas");
        let context = canvas.getContext('2d');
        canvas.width = imageData.width;
        canvas.height = imageData.height;

        context.putImageData(imageData, 0, 0);
        res(canvas.toDataURL("image/png"));
    });
}
function getPixel(imageData, x, y) {
    let start = ((imageData.width * y) + x) * 4;
    return imageData.data.slice(start, start+4);
}
function setPixel(imageData, x, y, value) {
    if (value.length != 4)
        value = [0, 0, 0, 0];
    let start = ((imageData.width * y) + x) * 4;
    for (let index = 0; index < 4; index++) {
        imageData.data[start+index]=value[index];        
    }
}
const channels={
    R:0,
    G:1,
    B:2,
    A:3
}
function toGrayScale(b64) {
    return new Promise((res, rej) => {
        //this follows colorimetric conversion
        //https://en.wikipedia.org/wiki/Grayscale#Converting_color_to_grayscale
        getImageData(b64).then((data) => {
            let result = new ImageData(data.width,data.height);
            
            for (let y = 0; y < data.height; y++) {
                for (let x = 0; x < data.width; x++) {
                    let pixel=getPixel(data,x,y);
                    let color= (.2126 * pixel[channels.R]+.7152*pixel[channels.R]+.0722*pixel[channels.B])/255;
                    if(color <=0.0031308)
                    {
                        color*=12.92;
                    }else{
                        color= 1.055 * color **(1/2.4) - 0.055
                    }
                    color*=255;
                    setPixel(result,x,y,[color,color,color,255]);
                }    
            }
            getBase64(result).then(res);
        });
    });
}