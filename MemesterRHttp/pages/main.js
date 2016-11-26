var $container = $('#container');
var $video = $container.find("main video");
var video = $video.get(0);

var $html = $('html');
var $playButton = $('.play');
var $volumeSlider = $('.volume');
var $progress = $('.progress');
var $volIcon = $('#muter');
var $currentTime = $('.currentTime');
var $totalTime = $('.totalTime');
var $usr = $('#username');
var $pwd = $('#password');

$progress.val(0);
$volumeSlider.val(video.volume * 100);

var changing = false;

var $dropdown = $('.dropdown');
var $button = $dropdown.find('button');

$button.click(function() {
    $dropdown.toggleClass('active');
});

$(".copy-button").click(copyToClipboard);
$("#logout").click(logout);
$("#login").click(login);

function copyToClipboard() {
    var $temp = $("<input>");
    $("body").append($temp);
    $temp.val(window.location.href).select();
    document.execCommand("copy");
    $temp.remove();
}

// $button.blur(function() {
//     if(!$dropdown.is(":hover"))
//         $dropdown.removeClass('active');
// });


$playButton.click(function () {
    playPause();
});

function playPause() {
    if (video.paused) {
        video.play()
    } else {
        video.pause()
    }
    $playButton.toggleClass('fa-pause');
}

$('#profile-btn').click(function () {
});

$video.on('play', function () {
    if(!$playButton.hasClass('fa-pause'))
        $playButton.toggleClass('fa-pause');
});

$video.on('pause', function () {
    if($playButton.hasClass('fa-pause'))
        $playButton.toggleClass('fa-pause');
});

$('.slider').click(function() {
    $(this).toggleClass('active');
    var auto = sessionStorage.getItem("ap");
    if (auto == null || auto == "null" || auto == "false")
        auto = "true";
    else
        auto = "false";
    sessionStorage.setItem("ap", auto);
});

$container.find('.fs-btn').click(function () {
    $container.toggleClass('fullscreen');
    $(".fs-btn").toggleClass('fa-compress');
    var fs = sessionStorage.getItem("fs");
    if (fs == null || fs == "null" || fs == "mini")
        fs = "full";
    else
        fs = "mini";
    sessionStorage.setItem("fs", fs);
});

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

$("#register").click(function () {
    location.href = "/pages/register.html";
});

video.onvolumechange = function () {
    var v = video.volume * 100;
    $volumeSlider.val(v);
    setVolIcon(v);
};

$video.on('timeupdate', function () {
    if(changing == false)
    {
        $progress.val((video.currentTime * 100) / video.duration);
        setCurrentTime();
    }
});

function setCurrentTime() {
    var time = video.currentTime;
    var minutes = Math.floor(time / 60);
    var seconds = Math.floor(time - minutes * 60);
    if (seconds.toString().length == 1) seconds = '0' + seconds;
    $currentTime.text(minutes + ':' + seconds);
}

$video.on('ended', function () {
    if($playButton.hasClass('fa-pause'))
        $playButton.toggleClass('fa-pause');
    if (sessionStorage.getItem("ap") == "true")
        window.location = "http://memester.club"
});

if (sessionStorage.getItem("fs") == "full"){
    $container.toggleClass('fullscreen');
    $(".fs-btn").toggleClass('fa-compress');

}

if (sessionStorage.getItem("ap") == "true"){
    $('.slider').toggleClass('active');
}

var vol = sessionStorage.getItem("vol");
if (vol != null && vol != "null"){
    var v = parseInt(vol);
    $volumeSlider.val(v);
    video.volume = v / 100;
    setVolIcon(v);
}

$video.click(function () {
    playPause();
});

video.onloadedmetadata = function() {
    var time = video.duration;
    console.log(time);
    var minutes = Math.floor(time / 60);
    var seconds = Math.floor(time - minutes * 60);
    if (seconds.toString().length == 1) seconds = '0' + seconds;
    $totalTime.text(minutes + ':' + seconds);
};

function login() {
    var usr = $usr.text();
    var pwd = $pwd.text();
    alert(usr + pwd);
}

function toggleMute() {
    var m = sessionStorage.getItem("mute");
    if (m == "true"){
        m = "false";
        video.muted = false;
        setVolIcon(video.volume * 100);
    }
    else {
        m = "true";
        video.muted = true;
        setVolIcon(0);
    }

    sessionStorage.setItem("mute", m);
}

$("#muter").click(toggleMute);

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
                location.reload();
                event.preventDefault();
                break;
        }
    }
    else if (event.keyCode == 13)
    {
        login();
        event.preventDefault();
    }
});

var $upvote = $("#upvote");
var $downvote = $("#downvote");

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
        // add vote POST action
    }
    else {
        $upvote.removeClass("voteSet");
        $upvote.addClass("hover-btn");
        // remove vote POST action
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
        // add vote POST action
    }
    else {
        $downvote.removeClass("voteSet");
        $downvote.addClass("hover-btn");
        // remove vote POST action
    }
}

$upvote.click(function () {
    upvote();
});
$downvote.click(downvote);

$html.mousedown(function () {
    if (!$dropdown.is(":hover") && $dropdown.hasClass('active'))
        $dropdown.removeClass('active');
});


function setVolIcon(vol) {
    if (vol == 0){
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
    if(ss == null || ss == "null" || ss == "")
        return false;
    return true

}

function login() {
    var username = $usr.val();
    var password = $pwd.val();
    alert(username +" "+ password);
    $.post("/login",{usr:username,pwd:password},function(data){
        if(data == "ok")
            sessionStorage.setItem("usr",username)
    });
    sessionStorage.setItem("usr",username);
    location.reload();
}

function logout() {
    sessionStorage.setItem("usr","");
    location.reload();
}

if(isLoggedin()){
    $("#account-div").css("display", "block");
    $("#login-div").css("display", "none");
}
