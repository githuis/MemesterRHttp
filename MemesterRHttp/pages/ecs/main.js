        var $container = $('#container');
        var $video = $container.find("main video");
        var video = $video.get(0);
        var $upvote = $("#upvote");
        var $downvote = $("#downvote");
        var $html = $('html');
        var $playButton = $('.play');
        var $volumeSlider = $('.volume');
        var $progress = $('.progress');
        var $volIcon = $('#muter');
        var $currentTime = $('.currentTime');
        var $totalTime = $('.totalTime');
        var $usr = $('#username');
        var $pwd = $('#password');
        var $slider = $('.slider');
        var $dropdown = $('.dropdown');
        var $button = $dropdown.find('button');
        var $fsBtn = $(".fs-btn");
        var $report = $("#report");
        var $thread = $("#thread");
        var changing = false;


        // Functions

        function copyToClipboard() {
            var $temp = $("<input>");
            $("body").append($temp);
            $temp.val(window.location.href).select();
            document.execCommand("copy");
            $temp.remove();
        }

        function playPause() {
            if (video.paused) {
                video.play()
            } else {
                video.pause()
            }
            $playButton.toggleClass('fa-pause');
        }

        function toggleMute() {
            var m = sessionStorage.getItem("mute");
            if (m == "true"){
                m = "false";
                video.muted = false;
            }
            else {
                m = "true";
                video.muted = true;
            }

            sessionStorage.setItem("mute", m);
        }

        function setCurrentTime() {
            var time = video.currentTime;
            var minutes = Math.floor(time / 60);
            var seconds = Math.floor(time - minutes * 60);
            if (seconds.toString().length == 1) seconds = '0' + seconds;
            $currentTime.text(minutes + ':' + seconds);
        }

        function setVolIcon(vol) {
            if (vol == 0 || video.muted){
                if (!$volIcon.hasClass("fa-volume-off"))
                    $volIcon.addClass("fa-volume-off");
                $volIcon.removeClass("fa-volume-down");
                $volIcon.removeClass("fa-volume-up");
            }

            else if (vol < 50){
                if (!$volIcon.hasClass("fa-volume-down"))
                    $volIcon.addClass("fa-volume-down");
                $volIcon.removeClass("fa-volume-off");
                $volIcon.removeClass("fa-volume-up");
            }
            else {
                if (!$volIcon.hasClass("fa-volume-up"))
                    $volIcon.addClass("fa-volume-up");
                $volIcon.removeClass("fa-volume-down");
                $volIcon.removeClass("fa-volume-off");
            }
        }

        function isLoggedin() {
            var ss = sessionStorage.getItem("usr");
            return !(ss == null || ss == "null" || ss == "");
        }

        function login() {
            var username = $usr.val();
            var password = $pwd.val();
            $.post("/login",{usr:username,pwd:password},function(data){
                if(data == "ok")
                {
                    sessionStorage.setItem("usr",username);
                    $("#accName").text(username);
                    location.reload();
                }
            });
        }

        function logout() {
            localStorage.setItem("usr","");
            sessionStorage.setItem("usr","");
            location.reload();
        }

        function upvote() {
            if ($downvote.hasClass("voteSet"))
            {
                $downvote.removeClass("voteSet");
                $downvote.addClass("hover-btn");
            }

            if (!$upvote.hasClass("voteSet"))
            {
                $upvote.addClass("voteSet");
                $upvote.removeClass("hover-btn");
                upDoot(true);
            }
            else {
                $upvote.removeClass("voteSet");
                $upvote.addClass("hover-btn");
                upDoot(false);
            }
        }

        function downvote() {
            if ($upvote.hasClass("voteSet"))
            {
                $upvote.removeClass("voteSet");
                $upvote.addClass("hover-btn");
            }

            if (!$downvote.hasClass("voteSet"))
            {
                $downvote.addClass("voteSet");
                $downvote.removeClass("hover-btn");
                downDoot(true);
            }
            else {
                $downvote.removeClass("voteSet");
                $downvote.addClass("hover-btn");
                downDoot(false);
            }
        }

        function upDoot(set) {
            $.post(window.location.pathname+"/vote",
                {
                    user:sessionStorage.getItem("usr"),
                    pass:sessionStorage.getItem("pwd"),
                    vote:set?"1":"0"
                }, function (data) {
                    if(data == "ok")
                        updateVotes(set?"1":"-1");
                } );
        }


        function downDoot(set) {
            $.post(window.location.pathname+"/vote",
                {
                    user:sessionStorage.getItem("usr"),
                    pass:sessionStorage.getItem("pwd"),
                    vote:set?"-1":"0"
                }, function (data) {
                    if(data == "ok")
                        updateVotes(set?"-1":"+1");
                } )
        }

        function updateVotes(amount) {
            $("#votes").innerText = parseInt($("#votes").innerText + amount)
        }

        function newMeme() {

            window.location.href = "/"
        }

        function toggleFullscreen() {
            $container.toggleClass('fullscreen');
            $report.toggleClass("fullscreen");
            $fsBtn.toggleClass('fa-compress');
            var fs = sessionStorage.getItem("fs");
            if (fs == null || fs == "null" || fs == "mini")
                fs = "full";
            else
                fs = "mini";
            sessionStorage.setItem("fs", fs);
        }

        function sendReport() {
            var reason = $("#report-form select").val();
            var email = $("#report-email").val();
            var message = $("#report-message").val();
            if(reason == "2" && (email == "" || message == "")) {
                alert("please input email and message");
                return;
            }
            $.post("/meme/"+mid+"/report",
                {
                    rn: reason,
                    reason: message,
                    email: email
                },function (data) {
                    if(data == "ok")
                        newMeme();
                }
            )
        }
        // Click handling

        $slider.click(function() {
            $slider.toggleClass('active');
            var auto = sessionStorage.getItem("ap");
            if (auto == null || auto == "null" || auto == "false")
                auto = "true";
            else
                auto = "false";
            sessionStorage.setItem("ap", auto);
        });

        $("#accPage").click(function () {
            location.href = "/user/" + sessionStorage.getItem("usr");
        });

        $("#register").click(function () {
            location.href = "/register";
        });

        $thread.click(function () {
            window.location.href = "/thread/" + encodeURIComponent($thread.text());
        });

        $button.click(function() {
            $dropdown.toggleClass('active');
        });

        $("#left").click(function () {
            history.back();
        });

        $report.click(function () {
            $("#report-form").css("display", "block");
            $("#report").css("display", "none");
        });

        $("#submit-report").click(sendReport);
        $fsBtn.click(toggleFullscreen);
        $("#right").click(newMeme);
        $(".copy-button").click(copyToClipboard);
        $("#logout").click(logout);
        $("#login").click(login);
        $playButton.click(playPause);
        $("#videoDiv").click(playPause);
        $volIcon.click(toggleMute);
        $upvote.click(upvote);
        $downvote.click(downvote);

        // Event handling

        $progress.on('input', function (e) {
            changing = true;
            video.currentTime = (video.duration / 100) * $(this).val();
            setCurrentTime();
            changing = false;
        });

        $volumeSlider.on('input', function (e) {
            var val = $(this).val();
            video.volume = val / 100;
            setVolIcon(val);
            sessionStorage.setItem("vol", val);
        });

        video.onvolumechange = function () {
            var v = video.volume * 100;
            $volumeSlider.val(v);
            setVolIcon(v);
        };

        video.onloadedmetadata = function() {
            var time = video.duration;
            var minutes = Math.floor(time / 60);
            var seconds = Math.floor(time - minutes * 60);
            if (seconds.toString().length == 1) seconds = '0' + seconds;
            $totalTime.text(minutes + ':' + seconds);
        };

        $video.on('timeupdate', function () {
            if(changing == false)
            {
                $progress.val((video.currentTime * 100) / video.duration);
                setCurrentTime();
            }
        });

        $video.on('ended', function () {
            if($playButton.hasClass('fa-pause'))
                $playButton.toggleClass('fa-pause');
            if (sessionStorage.getItem("ap") == "true")
                newMeme();
        });

        $video.on('play', function () {
            if(!$playButton.hasClass('fa-pause'))
                $playButton.toggleClass('fa-pause');
        });

        $video.on('pause', function () {
            if($playButton.hasClass('fa-pause'))
                $playButton.toggleClass('fa-pause');
        });

        $html.bind('keydown', function(event) {
            if (!$usr.is(":focus") && !$pwd.is(':focus')){
                switch (event.keyCode){
                    case 32:
                        playPause();
                        event.preventDefault();
                        break;
                    case 37:
                        history.back();
                        event.preventDefault();
                        break;
                    case 39:
                        newMeme();
                        event.preventDefault();
                        break;
                    case 27:
                        if (sessionStorage.getItem("fs") == "full"){
                            toggleFullscreen();
                            event.preventDefault();
                        }
                }
            }
            else if (event.keyCode == 13)
            {
                login();
                event.preventDefault();
            }
        });

        $html.mousedown(function () {
            if (!$dropdown.is(":hover") && $dropdown.hasClass('active'))
                $dropdown.removeClass('active');
        });




        // Page loading stuff

        if (sessionStorage.getItem("fs") == "full"){
            $container.toggleClass('fullscreen');
            $report.toggleClass("fullscreen");
            $(".fs-btn").toggleClass('fa-compress');
        }

        if (sessionStorage.getItem("ap") == "true"){
            $('.slider').toggleClass('active');
        }

        var vol = sessionStorage.getItem("vol");
        if (vol != null && vol != "null"){
            if (sessionStorage.getItem("mute") == "true"){
                video.muted = true;
            }
            var v = parseInt(vol);
            $volumeSlider.val(v);
            video.volume = v / 100;
            setVolIcon(v);
        }

        $progress.val(0);
        $volumeSlider.val(video.volume * 100);

        if(isLoggedin()){
            $("#account-div").css("display", "block");
            $("#login-div").css("display", "none");
            $("#accName").text(sessionStorage.getItem("usr"));
        }




