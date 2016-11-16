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

$progress.change(function (e) {
    console.log("asd");
    var progress = (video.duration / 100) * $(this).val();
    video.currentTime = progress;
});

$video.on('timeupdate', function () {
    $progress.val((video.currentTime * 100) / video.duration)
});
