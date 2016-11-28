var meme = document.getElementById("template");
var $content = $("#content");

function addMeme(id) {
    var clone = meme.content.cloneNode(true);
   clone.querySelector('img').src = "/thumbs/" + id + ".png";
    clone.querySelector('img').onclick = function () {
        window.location.href = "/memes/" + id;
    };

    $content.append(clone);
}

function getMemes() {
    $.get("/threads/"+tId,function (data) {
        if(data != "no"){
            for(i=0;i<data.length;i++)
            addMeme(data[i]);
        }
    })
}
getMemes();