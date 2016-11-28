var meme = document.getElementById("template");
var $content = $("#content");

function addMeme(title,id) {
    var clone = meme.content.cloneNode(true);
  //  clone.querySelector('img').src = "/thumbs/" + id + ".png";
    clone.querySelector('img').onclick = function () {
        window.location.href = "/memes/" + id;
    };

    $content.append(clone);
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