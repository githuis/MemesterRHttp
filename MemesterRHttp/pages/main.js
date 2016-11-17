var $container = $('#container');
var $video = $container.find("main video");
var video = $video.get(0);

var $playButton = $('.play');
var $fullscreen = $('.fullscreen');



$(".play").click(function () {
    if (video.paused) {
        video.play()
    } else {
        video.pause()
    }

    $playButton.toggleClass('fa-pause');
});


$container.find('.fullscreen').click(function () {
    $container.toggleClass('fullscreen');
});


var $progress = $('.progress');
$progress.val(0);
var changing = false;

$progress.change(function (e) {
    changing = true;
    var progress = (video.duration / 100) * $(this).val();
    video.currentTime = progress;
    changing = false;
});

$video.on('timeupdate', function () {
    if(changing == false)
    $progress.val((video.currentTime * 100) / video.duration)
});

$video.on('ended', function () {
    $playButton.toggleClass('fa-pause');
});



// Old but useful stuff

if (sessionStorage.getItem("fs") == "full"){
    $container.toggleClass('fullscreen');
}

