
    $('.slider').click(function() {
        $(this).toggleClass('active');
    });

    var progressBar = $('#progressBar');
    var video = document.getElementById("contentVideo");
    video.seekable = true;
    var progresschange = false;

    function loadFullscreenState(){
        var cState = sessionStorage.getItem("fs");
        if (cState == null || cState == "null" || cState == "mini"){
            $("#content").addClass("videoMinimized");
        }
        else{
            $("#content").addClass("videoFullscreen");
            $('#pageHeader').hide();
        }
    }
    loadFullscreenState();

    progressBar.on('input change', function() {
        console.log((video.duration / 100) * progressBar.val());
        video.currentTime = (video.duration / 100) * progressBar.val();
        console.log(progressBar.val());
    });



    function playPauseToggle() {
        if(video.paused){
            video.play();
            $('#playButton').addClass("pause");
            $('#playButton').removeClass("play");
        }
        else {
            video.pause();
            $('#playButton').addClass("play");
            $('#playButton').removeClass("pause");
        }
    }

    function toggleFullscreen() {
        var cState = sessionStorage.getItem("fs");
        if (cState == null || cState == "null"){
            cState = "full";
        }
        else if (cState == "full"){
            cState = "mini";
            $("#content").removeClass("videoFullscreen");
            $("#content").addClass("videoMinimized");
            $('#pageHeader').show();

        }
        else if (cState == "mini"){
            cState = "full";
            $("#content").removeClass("videoMinimized");
            $("#content").addClass("videoFullscreen");
            $('#pageHeader').hide();
        }
        sessionStorage.setItem("fs", cState);
    }

//    setInterval(function () {
//        if(progressBar.is(":focus"))
//            progressBar.val(video.duration/100*video.currentTime);
//        console.log(video.duration/100*video.currentTime);
//    },1000);
