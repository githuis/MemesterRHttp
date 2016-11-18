var $container = $('#container');
var $video = $container.find("main video");
var video = $video.get(0);

var $playButton = $('.play');
var $fullscreen = $('.fullscreen');
var $volumeSlider = $('.volume');
var $progress = $('.progress');


$progress.val(0);
$volumeSlider.val(video.volume * 100);

var changing = false;


$(".play").click(function () {
    if (video.paused) {
        video.play()
    } else {
        video.pause()
    }
    $playButton.toggleClass('fa-pause');
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

$progress.change(function (e) {
    changing = true;
    var progress = (video.duration / 100) * $(this).val();
    video.currentTime = progress;
    changing = false;
});

$volumeSlider.change(function (e) {
    video.volume = $(this).val() / 100;
    sessionStorage.setItem("vol",video.volume);
});

video.onvolumechange = function () {
    $volumeSlider.val(video.volume * 100);
};

$video.on('timeupdate', function () {
    if(changing == false)
    $progress.val((video.currentTime * 100) / video.duration)
});

$video.on('ended', function () {
    if($playButton.hasClass('fa-pause'))
        $playButton.toggleClass('fa-pause');
    if (sessionStorage.getItem("ap") == "true")
        window.location = "memester.club"
});

if (sessionStorage.getItem("fs") == "full"){
    $container.toggleClass('fullscreen');
}

if (sessionStorage.getItem("ap") == "true"){
    $('.slider').toggleClass('active');
}
