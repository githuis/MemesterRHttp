var meme = document.getElementById("template");
var $content = $("#usermemes");
var user = sessionStorage.getItem("usr");

function addMeme(id) {
    var clone = meme.content.cloneNode(true);
    //  clone.querySelector('img').src = "/thumbs/" + id + ".png";
    clone.querySelector('img').onclick = function () {
        window.location.href = "/memes/" + id;
    };

    $content.append(clone);
}

function getUserMemes(page) {
    $.get("/user/"+user+"/liked?p="+page, function (data) {
         if (data != "no"){
             for(i = 0; i < data.length;i++){
                 addMeme(data[i])
             }
         }
    })
}

addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");
addMeme("hej jens", "ldskjnfasd34209p");