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
            for (var i = 0; i < threads.length; i++){
                addMeme(threads[i]);
            }
            // $.post("/thread/"+tId,function (data) {
            //     console.log(data != "no");
            //     if(data != "no"){
            //         for(i=0;i<data.length;i++)
            //         addMeme(data[i]);
            //     }
            // })
        }
        getMemes();