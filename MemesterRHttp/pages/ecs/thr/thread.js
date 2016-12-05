        var meme = document.getElementById("template");
        var $content = $("#content");

        function addMeme(id) {
            var clone = meme.content.cloneNode(true);
            clone.querySelector('img').src = "/thumbs/" + id + ".png";
            clone.querySelector('img').onclick = function () {
                window.location.href = "/meme/" + id;
            };

            $content.append(clone);
        }

        function getMemes() {
            $.post("/thread/"+encodeURIComponent(tId),function (data) {
                console.log(data != "no");
                if(data != "no"){
                    for(i=0;i<data.length;i++)
                    addMeme(data[i]);
                }
            })
        }
        getMemes();