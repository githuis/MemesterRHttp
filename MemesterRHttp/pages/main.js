var $container = $('#container');
var $video = $container.find("main video");
var video = $video.get(0);

var $playButton = $('.play');
var $fullscreen = $('.fullscreen');
var $volumeSlider = $('.volume');
var $progress = $('.progress');
var $volIcon = $('.player-controls > i');


$progress.val(0);
$volumeSlider.val(video.volume * 100);

var changing = false;

var $dropdown = $('.dropdown');
var $button = $dropdown.find('button');

$button.click(function() {
    $dropdown.toggleClass('active');
});

$button.blur(function() {
    if(!$(".dropdown").is(":hover"))
        $dropdown.removeClass('active');
});


$(".play").click(function () {
    if (video.paused) {
        video.play()
    } else {
        video.pause()
    }
    $playButton.toggleClass('fa-pause');
});

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

$container.find('.fullscreen').click(function () {
    $container.toggleClass('fullscreen');
    var fs = sessionStorage.getItem("fs");
    if (fs == null || fs == "null" || fs == "mini")
        fs = "full";
    else
        fs = "mini";
    sessionStorage.setItem("fs", fs);

});

$progress.on('input', function (e) {
    changing = true;
    var progress = (video.duration / 100) * $(this).val();
    video.currentTime = progress;
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

$video.on('timeupdate', function () {
    if(changing == false)
        $progress.val((video.currentTime * 100) / video.duration)
});

$video.on('ended', function () {
    if($playButton.hasClass('fa-pause'))
        $playButton.toggleClass('fa-pause');
    if (sessionStorage.getItem("ap") == "true")
        window.location = "http://memester.club"
});

if (sessionStorage.getItem("fs") == "full"){
    $container.toggleClass('fullscreen');
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

function setVolIcon(vol) {
    if (vol == 0){
        if (!$volIcon.hasClass("fa-volume-off"))
            $volIcon.toggleClass("fa-volume-off");
        $volIcon.removeClass("fa-volume-down");
        $volIcon.removeClass("fa-volume-up");
    }

    else if (vol < 50){
        if (!$volIcon.hasClass("fa-volume-down"))
            $volIcon.toggleClass("fa-volume-down");
        $volIcon.removeClass("fa-volume-off");
        $volIcon.removeClass("fa-volume-up");
    }
    else {
        if (!$volIcon.hasClass("fa-volume-up"))
            $volIcon.toggleClass("fa-volume-up");
        $volIcon.removeClass("fa-volume-down");
        $volIcon.removeClass("fa-volume-off");
    }
}
