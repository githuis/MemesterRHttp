        var meme = document.getElementById("template");
        var $content = $("#content");

        function addMeme(id) {
            var clone = meme.content.cloneNode(true);
            clone.querySelector('img').src = "/thumbs/" + id + ".jpg";
            clone.querySelector('a').href = "/meme/" + id;
            $content.append(clone);
        }
        for (var i = 0; i < threads.length; i++){
            addMeme(threads[i]);
        }